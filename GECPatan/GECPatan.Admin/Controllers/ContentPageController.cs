using System.Security.Claims;
using System.Text.RegularExpressions;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principal")]
    public class ContentPageController : Controller
    {
        private const string AuditModule = "ContentPage";
        private const string SuperAdminRole = "SuperAdmin";
        private const string ContentPageIcon = "fa-file-alt";
        private const string FormView = "Form";   // shared by Create and Edit

        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<ContentPageController> _logger;

        public ContentPageController(
            ApplicationDbContext context,
            NotificationService notify,
            IFileStorageService fileStorage,
            ILogger<ContentPageController> logger)
        {
            _context = context;
            _notify = notify;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Content Pages";

            var pages = await _context.ContentPages
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new ContentPageListVM
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    CreatedBy = p.CreatedBy,
                    CreatedDate = p.CreatedDate,
                    IsVisible = p.IsVisible
                })
                .ToListAsync();

            return View(pages);
        }

        // ── CREATE GET ────────────────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Page";
            return View(FormView, new ContentPageFormVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContentPageFormVM model)
        {
            ViewData["Title"] = "Create Page";

            model.Slug = GenerateSlug(
                string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            // Show the cleaned slug in the form, not the raw value that was posted.
            ModelState.Remove(nameof(model.Slug));

            if (string.IsNullOrEmpty(model.Slug))
            {
                ModelState.AddModelError(
                    nameof(model.Slug),
                    "Could not generate a URL slug. Please type one using English letters or numbers.");
            }
            else if (await _context.ContentPages.AnyAsync(p => p.Slug == model.Slug))
            {
                ModelState.AddModelError(
                    nameof(model.Slug),
                    "This slug is already in use. Please choose a different title or slug.");
            }

            if (!ModelState.IsValid)
                return View(FormView, model);

            var userName = User.Identity?.Name ?? "Admin";

            using var transaction = await _context.Database.BeginTransactionAsync();
            var newlySavedFiles = new List<string>();

            try
            {
                var page = new ContentPage
                {
                    Title = model.Title,
                    Slug = model.Slug,
                    HtmlContent = model.HtmlContent ?? string.Empty,
                    CreatedBy = userName,
                    IsVisible = model.IsVisible
                };

                _context.ContentPages.Add(page);

                // Save first to obtain the database-generated page Id.
                await _context.SaveChangesAsync();

                // Save carousel images.
                var (skipped, carouselSavedPaths) = await SaveCarouselImages(
                    page.Id, page.Slug, model.CarouselImages);

                newlySavedFiles.AddRange(carouselSavedPaths);

                // Save the single Title Image, if provided.
                if (model.TitleImage != null && model.TitleImage.Length > 0)
                {
                    var titleImageResult = await _fileStorage.SaveAsync(
                        model.TitleImage,
                        $"content-pages-{page.Slug}",
                        FileCategory.Image);

                    if (titleImageResult.Success &&
                        !string.IsNullOrWhiteSpace(titleImageResult.RelativePath))
                    {
                        page.TitleImageUrl = titleImageResult.RelativePath;
                        newlySavedFiles.Add(titleImageResult.RelativePath);
                    }
                    else
                    {
                        skipped.Add(
                            $"Title Image: {titleImageResult.ErrorMessage ?? "The image could not be saved."}");
                    }
                }

                AddAuditLog("Created", page.Id, page.Title);

                // Persist title image path, carousel rows, and audit entry together.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Content page {Id} '{Title}' created by {User}",
                    page.Id, page.Title, User.Identity?.Name);

                // Notification is best-effort; the page has already been committed.
                try
                {
                    await _notify.SendAsync(
                        title: $"Content Page Published: {page.Title}",
                        message: $"URL: /page/{page.Slug}",
                        module: AuditModule,
                        icon: ContentPageIcon,
                        color: "primary",
                        link: $"/ContentPage/Edit/{page.Id}",
                        forRole: SuperAdminRole);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send creation notification for content page {Id}",
                        page.Id);
                }

                TempData["Success"] =
                    $"Page '{page.Title}' created. URL: /page/{page.Slug}";

                if (skipped.Any())
                    TempData["Warning"] =
                        "Some images were skipped: " + string.Join(" | ", skipped);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex, "Database error while creating content page '{Title}'", model.Title);

                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(
                    string.Empty,
                    "Could not save the page due to a database error. Please try again.");

                return View(FormView, model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex, "Unexpected error while creating content page '{Title}'", model.Title);

                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while creating the page. Please try again.");

                return View(FormView, model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Page";

            // AsNoTracking: the page is only read to fill a form model.
            var page = await _context.ContentPages
                .AsNoTracking()
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (page == null)
                return NotFound();

            var model = new ContentPageFormVM
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                HtmlContent = page.HtmlContent,
                IsVisible = page.IsVisible,
                GeneratedUrl = $"/page/{page.Slug}",

                // This is the saved single Title Image path for the form.
                TitleImageUrl = page.TitleImageUrl,

                ExistingImages = page.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    })
                    .ToList()
            };

            return View(FormView, model);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentPageFormVM model)
        {
            ViewData["Title"] = "Edit Page";

            if (id != model.Id)
                return BadRequest();

            model.Slug = GenerateSlug(
                string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            // Show the cleaned slug in the form, not the raw value that was posted.
            ModelState.Remove(nameof(model.Slug));

            if (string.IsNullOrEmpty(model.Slug))
            {
                ModelState.AddModelError(
                    nameof(model.Slug),
                    "Could not generate a URL slug. Please type one using English letters or numbers.");
            }
            else if (await _context.ContentPages.AnyAsync(
                p => p.Slug == model.Slug && p.Id != id && !p.IsDeleted))
            {
                ModelState.AddModelError(
                    nameof(model.Slug),
                    "This slug is already in use by another page.");
            }

            // Check the page exists and retrieve the saved Title Image path.
            var page = await _context.ContentPages
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (page == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEditModelData(model, id, page.TitleImageUrl);
                return View(FormView, model);
            }

            var oldTitleImagePath = page.TitleImageUrl;
            string? newTitleImagePath = null;

            page.Title = model.Title;
            page.Slug = model.Slug!;
            page.HtmlContent = model.HtmlContent ?? string.Empty;
            page.IsVisible = model.IsVisible;

            var (skipped, newlySavedFiles) = await SaveCarouselImages(
                page.Id, page.Slug, model.CarouselImages);

            // Upload a replacement only when the admin selected a new file.
            if (model.TitleImage != null && model.TitleImage.Length > 0)
            {
                var titleImageResult = await _fileStorage.SaveAsync(
                    model.TitleImage,
                    $"content-pages-{page.Slug}",
                    FileCategory.Image);

                if (titleImageResult.Success &&
                    !string.IsNullOrWhiteSpace(titleImageResult.RelativePath))
                {
                    newTitleImagePath = titleImageResult.RelativePath;
                    page.TitleImageUrl = newTitleImagePath;
                    newlySavedFiles.Add(newTitleImagePath);
                }
                else
                {
                    skipped.Add(
                        $"Title Image: {titleImageResult.ErrorMessage ?? "The image could not be saved."}");
                }
            }

            try
            {
                AddAuditLog("Updated", page.Id, page.Title);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing content page {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);

                // Rebuild view data and show the old saved image after failure.
                await PopulateEditModelData(model, id, oldTitleImagePath);

                ModelState.AddModelError(
                    string.Empty,
                    "Could not save the page due to a database error. Please try again.");

                return View(FormView, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing content page {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);

                await PopulateEditModelData(model, id, oldTitleImagePath);

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while saving the page. Please try again.");

                return View(FormView, model);
            }

            // Delete the previous image only after the replacement path was saved.
            if (!string.IsNullOrWhiteSpace(newTitleImagePath) &&
                !string.IsNullOrWhiteSpace(oldTitleImagePath))
            {
                TryDeleteFile(oldTitleImagePath, "replaced content page title image");
            }

            _logger.LogInformation(
                "Content page {Id} '{Title}' edited by {User}",
                page.Id, page.Title, User.Identity?.Name);

            TempData["Success"] = $"Page updated. URL: /page/{page.Slug}";

            if (skipped.Any())
                TempData["Warning"] =
                    "Some images were skipped: " + string.Join(" | ", skipped);

            if (User.IsInRole(AppRoles.ContentEditor))
                return RedirectToAction("Index", "Home");

            return RedirectToAction(nameof(Index));
        }

        // Repopulates saved image and carousel details when Edit POST returns
        // the form because validation or persistence failed.
        private async Task PopulateEditModelData(
            ContentPageFormVM model,
            int pageId,
            string? titleImageUrl)
        {
            var existing = await _context.ContentPages
                .AsNoTracking()
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == pageId && !x.IsDeleted);

            model.TitleImageUrl = titleImageUrl;
            model.GeneratedUrl = existing == null
                ? string.Empty
                : $"/page/{existing.Slug}";

            model.ExistingImages = existing?.Images
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ContentPageImageVM
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    Caption = i.Caption,
                    DisplayOrder = i.DisplayOrder
                })
                .ToList() ?? new List<ContentPageImageVM>();
        }

        // ── DELETE ONE CAROUSEL IMAGE ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int pageId)
        {
            var img = await _context.ContentPageImages.FindAsync(id);
            if (img == null || img.ContentPageId != pageId)
                return NotFound();

            var imagePath = img.ImageUrl;

            try
            {
                _context.ContentPageImages.Remove(img);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete content page image {ImageId} for page {PageId}",
                    id, pageId);

                TempData["Error"] = "Could not remove the image. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = pageId });
            }

            TryDeleteFile(imagePath, "deleted content page image");
            TempData["Success"] = "Image removed.";

            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var page = await _context.ContentPages.FindAsync(id);
            if (page == null || page.IsDeleted)
                return NotFound();

            page.IsVisible = !page.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle visibility for content page {Id}", id);
                TempData["Error"] = "Could not update visibility. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                $"'{page.Title}' " + (page.IsVisible ? "published" : "hidden") + ".";

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var page = await _context.ContentPages.FindAsync(id);
            if (page == null || page.IsDeleted)
                return NotFound();

            page.IsDeleted = true;

            try
            {
                AddAuditLog("Deleted", page.Id, page.Title);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete content page {Id}", id);
                TempData["Error"] = "Could not delete the page. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation(
                "Content page {Id} '{Title}' deleted by {User}",
                page.Id, page.Title, User.Identity?.Name);

            TempData["Success"] = $"Page '{page.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── PREVIEW (renders TinyMCE content + carousel) ──
        public async Task<IActionResult> Preview(int id)
        {
            var page = await _context.ContentPages
                .AsNoTracking()
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (page == null)
                return NotFound();

            ViewData["Title"] = page.Title;
            return View(page);
        }

        // ── HELPERS ───────────────────────────────────────
        private static string GenerateSlug(string text)
        {
            text = (text ?? string.Empty).ToLowerInvariant();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            text = Regex.Replace(text, @"-+", "-");
            text = text.Trim('-');
            return text;
        }

        /// <summary>
        /// Saves carousel images through the centralized file service.
        /// Invalid files are skipped and reported. Returns paths saved so
        /// callers can clean them up if the database save fails.
        /// </summary>
        private async Task<(List<string> Skipped, List<string> SavedPaths)> SaveCarouselImages(
            int contentPageId,
            string slug,
            List<IFormFile>? files)
        {
            var skipped = new List<string>();
            var savedPaths = new List<string>();

            if (files == null || files.Count == 0)
                return (skipped, savedPaths);

            var folder = $"content-pages-{slug}";

            var nextOrder = await _context.ContentPageImages
                .Where(i => i.ContentPageId == contentPageId)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync() ?? -1;

            nextOrder++;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                var result = await _fileStorage.SaveAsync(
                    file, folder, FileCategory.Image);

                if (!result.Success ||
                    string.IsNullOrWhiteSpace(result.RelativePath))
                {
                    skipped.Add(
                        $"{file.FileName}: {result.ErrorMessage ?? "The image could not be saved."}");
                    continue;
                }

                savedPaths.Add(result.RelativePath);

                _context.ContentPageImages.Add(new ContentPageImage
                {
                    ContentPageId = contentPageId,
                    ImageUrl = result.RelativePath,
                    DisplayOrder = nextOrder++
                });
            }

            return (skipped, savedPaths);
        }

        /// <summary>
        /// Deletes one file without throwing. A failed delete is logged for
        /// later cleanup and does not take down the request.
        /// </summary>
        private void TryDeleteFile(string? path, string context)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                if (!_fileStorage.Delete(path))
                {
                    _logger.LogWarning(
                        "File delete returned false for {Path} ({Context})",
                        path, context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Failed to delete file {Path} ({Context})", path, context);
            }
        }

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDeleteFile(path, "orphan cleanup");
        }

        /// <summary>
        /// Stages an audit log entry without saving. It is persisted with the
        /// entity changes in the caller's SaveChangesAsync operation.
        /// </summary>
        private void AddAuditLog(string action, int recordId, string recordName)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var role = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = userName,
                UserRole = role,
                Action = action,
                Module = AuditModule,
                RecordId = recordId,
                RecordName = recordName,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}