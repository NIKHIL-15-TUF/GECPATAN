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
            return View(new ContentPageCreateVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContentPageCreateVM model)
        {
            ViewData["Title"] = "Create Page";

            model.Slug = GenerateSlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            if (await _context.ContentPages.AnyAsync(p => p.Slug == model.Slug))
            {
                ModelState.AddModelError(nameof(model.Slug),
                    "This slug is already in use. Please choose a different title or slug.");
            }

            if (!ModelState.IsValid) return View(model);

            var userName = User.Identity?.Name ?? "Admin";

            // Two SaveChangesAsync calls are needed — carousel images and the
            // audit entry both need the page's real, database-generated Id,
            // which only exists after the first insert. The transaction gives
            // the all-or-nothing guarantee instead.
            using var transaction = await _context.Database.BeginTransactionAsync();
            var newlySavedFiles = new List<string>();
            try
            {
                var page = new ContentPage
                {
                    Title = model.Title,
                    Slug = model.Slug,
                    HtmlContent = model.HtmlContent,
                    CreatedBy = userName,
                    IsVisible = model.IsVisible
                };

                _context.ContentPages.Add(page);
                await _context.SaveChangesAsync();

                var (skipped, savedPaths) = await SaveCarouselImages(page.Id, page.Slug, model.CarouselImages);
                newlySavedFiles.AddRange(savedPaths);

                AddAuditLog("Created", page.Id, page.Title);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Content page {Id} '{Title}' created by {User}", page.Id, page.Title, User.Identity?.Name);

                // Best-effort: the page is already saved at this point, so a
                // notification failure is logged, not surfaced as a request error.
                try
                {
                    await _notify.SendAsync(
                        title: $"Content Page Published: {page.Title}",
                        message: $"URL: /page/{page.Slug}",
                        module: AuditModule,
                        icon: ContentPageIcon,
                        color: "primary",
                        link: $"/ContentPage/Edit/{page.Id}",
                        forRole: SuperAdminRole
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send creation notification for content page {Id}", page.Id);
                }

                TempData["Success"] = $"Page '{page.Title}' created. URL: /page/{page.Slug}";
                if (skipped.Any())
                    TempData["Warning"] = "Some images were skipped: " + string.Join(" | ", skipped);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while creating content page '{Title}'", model.Title);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the page due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while creating content page '{Title}'", model.Title);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the page. Please try again.");
                return View(model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Page";
            var p = await _context.ContentPages
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (p == null) return NotFound();

            return View(new ContentPageEditVM
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                HtmlContent = p.HtmlContent,
                IsVisible = p.IsVisible,
                GeneratedUrl = $"/page/{p.Slug}",
                ExistingImages = p.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    }).ToList()
            });
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentPageEditVM model)
        {
            ViewData["Title"] = "Edit Page";

            model.Slug = GenerateSlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            if (await _context.ContentPages.AnyAsync(p => p.Slug == model.Slug && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Slug), "This slug is already in use by another page.");
            }

            if (!ModelState.IsValid)
            {
                var existing = await _context.ContentPages.Include(x => x.Images).AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                model.ExistingImages = existing?.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    }).ToList() ?? new List<ContentPageImageVM>();
                return View(model);
            }

            var p = await _context.ContentPages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (p == null) return NotFound();

            p.Title = model.Title;
            p.Slug = model.Slug;
            p.HtmlContent = model.HtmlContent;
            p.IsVisible = model.IsVisible;

            var (skipped, newlySavedFiles) = await SaveCarouselImages(p.Id, p.Slug, model.CarouselImages);

            try
            {
                AddAuditLog("Updated", p.Id, p.Title);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing content page {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the page due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing content page {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the page. Please try again.");
                return View(model);
            }

            _logger.LogInformation("Content page {Id} '{Title}' edited by {User}", p.Id, p.Title, User.Identity?.Name);
            TempData["Success"] = $"Page updated. URL: /page/{p.Slug}";
            if (skipped.Any())
                TempData["Warning"] = "Some images were skipped: " + string.Join(" | ", skipped);

            if (User.IsInRole(AppRoles.ContentEditor))
                return RedirectToAction("Index", "Home");
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ONE CAROUSEL IMAGE ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int pageId)
        {
            var img = await _context.ContentPageImages.FindAsync(id);
            if (img == null || img.ContentPageId != pageId) return NotFound();

            var imagePath = img.ImageUrl;

            try
            {
                _context.ContentPageImages.Remove(img);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete content page image {ImageId} for page {PageId}", id, pageId);
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
            var p = await _context.ContentPages.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVisible = !p.IsVisible;

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

            TempData["Success"] = $"'{p.Title}' " + (p.IsVisible ? "published" : "hidden") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.ContentPages.FindAsync(id);
            if (p == null) return NotFound();
            p.IsDeleted = true;

            try
            {
                AddAuditLog("Deleted", p.Id, p.Title);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete content page {Id}", id);
                TempData["Error"] = "Could not delete the page. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Content page {Id} '{Title}' deleted by {User}", p.Id, p.Title, User.Identity?.Name);
            TempData["Success"] = $"Page '{p.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── PREVIEW (renders TinyMCE content + carousel) ──
        public async Task<IActionResult> Preview(int id)
        {
            var p = await _context.ContentPages
                .Include(x => x.Images)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (p == null) return NotFound();
            ViewData["Title"] = p.Title;
            return View(p);
        }

        // ── HELPERS ───────────────────────────────────────
        private static string GenerateSlug(string text)
        {
            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            text = Regex.Replace(text, @"-+", "-");
            text = text.Trim('-');
            return text;
        }

        /// <summary>
        /// Saves carousel images through the centralized file service, grouped
        /// under a per-page folder (content-pages-{slug}) so a bad image on one
        /// page doesn't require guessing which folder it landed in. Bad files
        /// are skipped and reported rather than silently dropped or failing
        /// the whole save, matching the multi-file pattern used elsewhere.
        /// Returns the skip messages and the list of paths actually written,
        /// so the caller can clean them up if the surrounding save fails.
        /// </summary>
        private async Task<(List<string> Skipped, List<string> SavedPaths)> SaveCarouselImages(
            int contentPageId, string slug, List<IFormFile>? files)
        {
            var skipped = new List<string>();
            var savedPaths = new List<string>();
            if (files == null || files.Count == 0) return (skipped, savedPaths);

            var folder = $"content-pages-{slug}";

            var nextOrder = await _context.ContentPageImages
                .Where(i => i.ContentPageId == contentPageId)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync() ?? -1;
            nextOrder++;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                var result = await _fileStorage.SaveAsync(file, folder, FileCategory.Image);
                if (!result.Success)
                {
                    skipped.Add($"{file.FileName}: {result.ErrorMessage}");
                    continue;
                }

                savedPaths.Add(result.RelativePath!);
                _context.ContentPageImages.Add(new ContentPageImage
                {
                    ContentPageId = contentPageId,
                    ImageUrl = result.RelativePath!,
                    DisplayOrder = nextOrder++
                });
            }

            return (skipped, savedPaths);
        }

        /// <summary>
        /// Deletes a single file and never throws — a failed delete here
        /// should never take down the request; it just gets logged so it can
        /// be cleaned up manually.
        /// </summary>
        private void TryDeleteFile(string? path, string context)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (!_fileStorage.Delete(path))
                    _logger.LogWarning("File delete returned false for {Path} ({Context})", path, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {Path} ({Context})", path, context);
            }
        }

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDeleteFile(path, "orphan cleanup");
        }

        /// <summary>
        /// Stages an audit log entry on the context without saving. Callers
        /// commit it together with their own entity changes in a single
        /// SaveChangesAsync call, so the audit entry and the change it
        /// describes are always persisted atomically.
        /// </summary>
        private void AddAuditLog(string action, int recordId, string recordName)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = userName,
                UserRole = role,
                Action = action,
                Module = AuditModule,
                RecordId = recordId,
                RecordName = recordName,
                Timestamp = DateTime.UtcNow // stored in UTC; convert to local time when displaying
            });
        }
    }
}