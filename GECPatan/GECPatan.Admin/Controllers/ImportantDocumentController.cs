using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class ImportantDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly NotificationService _notify;
        private readonly ILogger<ImportantDocumentController> _logger;
        private const string DocsFolder = "documents";
        private const string BannerFolder = "document-banners";

        public ImportantDocumentController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            NotificationService notify,
            ILogger<ImportantDocumentController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _notify = notify;
            _logger = logger;
        }

        // ══════════════════ DOCUMENT PAGES (top level) ══════════════════

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Documents";
            var pages = await _context.DocumentPages
                .Include(p => p.YearSections).ThenInclude(y => y.Files)
                .Include(p => p.Files)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            var vms = pages.Select(p => new DocumentPageVM
            {
                Id = p.Id,
                Title = p.Title,
                ExistingBannerPath = p.TitleBannerImagePath,
                TableView = p.TableView,
                HasYearSections = p.HasYearSections,
                DisplayOrder = p.DisplayOrder,
                IsVisible = p.IsVisible,
                YearCount = p.YearSections.Count(y => !y.IsDeleted),
                FileCount = p.HasYearSections
                    ? p.YearSections.SelectMany(y => y.Files).Count(f => !f.IsDeleted)
                    : p.Files.Count(f => !f.IsDeleted)
            }).ToList();

            return View(vms);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Document Page";
            return View(new DocumentPageVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentPageVM model, IFormFile? TitleBannerImage)
        {
            ViewData["Title"] = "Add Document Page";
            if (!ModelState.IsValid) return View(model);

            string? bannerPath = null;
            if (TitleBannerImage != null && TitleBannerImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleBannerImage, BannerFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleBannerImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                bannerPath = uploadResult.RelativePath;
            }

            int maxOrder = await _context.DocumentPages
                .Select(p => (int?)p.DisplayOrder).MaxAsync() ?? -1;

            var page = new DocumentPage
            {
                Title = model.Title,
                TitleBannerImagePath = bannerPath,
                TableView = model.TableView,
                // Group-by-year is decided once, right here, and never touched again.
                HasYearSections = model.HasYearSections,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.DocumentPages.Add(page);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Document page {Id} '{Title}' created (HasYearSections={HasYearSections})",
                    page.Id, page.Title, page.HasYearSections);
                TempData["Success"] = "Document page added.";
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(bannerPath);
                _logger.LogError(ex, "Database error creating document page '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the document page. Please try again.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Document Page";
            var p = await _context.DocumentPages.FindAsync(id);
            if (p == null) return NotFound();

            return View(new DocumentPageVM
            {
                Id = p.Id,
                Title = p.Title,
                ExistingBannerPath = p.TitleBannerImagePath,
                TableView = p.TableView,
                HasYearSections = p.HasYearSections, // shown disabled/read-only in the view
                DisplayOrder = p.DisplayOrder,
                IsVisible = p.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentPageVM model, IFormFile? TitleBannerImage)
        {
            ViewData["Title"] = "Edit Document Page";
            if (!ModelState.IsValid) return View(model);

            var p = await _context.DocumentPages.FindAsync(id);
            if (p == null) return NotFound();

            bool replacingBanner = TitleBannerImage != null && TitleBannerImage.Length > 0;
            string? newBannerPath = null;
            if (replacingBanner)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleBannerImage, BannerFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleBannerImage), uploadResult.ErrorMessage!);
                    model.ExistingBannerPath = p.TitleBannerImagePath;
                    return View(model);
                }
                newBannerPath = uploadResult.RelativePath;
            }

            string? previousBannerPath = p.TitleBannerImagePath;

            p.Title = model.Title;
            p.TableView = model.TableView;
            p.DisplayOrder = model.DisplayOrder;
            p.IsVisible = model.IsVisible;
            // NOTE: p.HasYearSections is intentionally never assigned here. This setting
            // is locked at creation — the model's HasYearSections value (whatever the
            // client posted) is deliberately ignored to prevent tampering via POST.

            if (replacingBanner)
            {
                p.TitleBannerImagePath = newBannerPath;
            }
            else if (model.RemoveBanner)
            {
                p.TitleBannerImagePath = null;
            }

            try
            {
                await _context.SaveChangesAsync();

                // Old banner removed only after the new state is safely persisted.
                if (replacingBanner) _fileStorage.Delete(previousBannerPath);
                else if (model.RemoveBanner) _fileStorage.Delete(previousBannerPath);

                TempData["Success"] = "Document page updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingBanner) _fileStorage.Delete(newBannerPath);
                _logger.LogError(ex, "Database error updating document page {Id}", id);
                ModelState.AddModelError(string.Empty, "Unable to save. Please try again.");
                model.ExistingBannerPath = previousBannerPath;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var p = await _context.DocumentPages.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVisible = !p.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.DocumentPages
                .Include(x => x.YearSections).ThenInclude(y => y.Files)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            bool hasFiles = p.HasYearSections
                ? p.YearSections.Any(y => y.Files.Any(f => !f.IsDeleted))
                : p.Files.Any(f => !f.IsDeleted);

            if (hasFiles)
            {
                TempData["Error"] = $"Cannot delete '{p.Title}' — it still has documents. Remove them first.";
                return RedirectToAction(nameof(Index));
            }

            p.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Document page deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════ YEAR SECTIONS (middle level — year-grouped pages only) ══════════════════

        public async Task<IActionResult> YearSections(int pageId)
        {
            var page = await _context.DocumentPages.FindAsync(pageId);
            if (page == null) return NotFound();

            if (!page.HasYearSections)
                return RedirectToAction(nameof(Files), new { pageId });

            ViewData["Title"] = $"{page.Title} — Years";
            ViewBag.PageId = pageId;
            ViewBag.PageTitle = page.Title;

            var years = await _context.DocumentYearSections
                .Include(y => y.Files)
                .Where(y => y.PageId == pageId && !y.IsDeleted)
                .OrderByDescending(y => y.Year)
                .ToListAsync();

            return View(years.Select(y => new DocumentYearSectionVM
            {
                Id = y.Id,
                PageId = y.PageId,
                Year = y.Year,
                DisplayOrder = y.DisplayOrder,
                FileCount = y.Files.Count(f => !f.IsDeleted)
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddYearSection(DocumentYearSectionVM model)
        {
            var page = await _context.DocumentPages.FindAsync(model.PageId);
            if (page == null)
            {
                TempData["Error"] = "Invalid document page.";
                return RedirectToAction(nameof(Index));
            }
            if (!page.HasYearSections)
            {
                TempData["Error"] = "This page is not configured for year grouping.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Year is required.";
                return RedirectToAction(nameof(YearSections), new { pageId = model.PageId });
            }

            int maxOrder = await _context.DocumentYearSections
                .Where(y => y.PageId == model.PageId)
                .Select(y => (int?)y.DisplayOrder).MaxAsync() ?? -1;

            _context.DocumentYearSections.Add(new DocumentYearSection
            {
                PageId = model.PageId,
                Year = model.Year,
                DisplayOrder = maxOrder + 1
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Year section added.";
            return RedirectToAction(nameof(YearSections), new { pageId = model.PageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteYearSection(int id, int pageId)
        {
            var y = await _context.DocumentYearSections
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (y == null) return NotFound();

            if (y.Files.Any(f => !f.IsDeleted))
            {
                TempData["Error"] = $"Cannot delete '{y.Year}' — it still has files. Remove them first.";
                return RedirectToAction(nameof(YearSections), new { pageId });
            }

            y.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Year section deleted.";
            return RedirectToAction(nameof(YearSections), new { pageId });
        }

        // ══════════════════ FILES (leaf level — either year-bound or direct-to-page) ══════════════════

        // Supports two routing modes:
        //   Files?yearSectionId=5   → files inside a year section (year-grouped page)
        //   Files?pageId=5          → files uploaded directly under a page (non-year-grouped page)
        public async Task<IActionResult> Files(int? pageId, int? yearSectionId)
        {
            if (yearSectionId.HasValue)
            {
                var year = await _context.DocumentYearSections
                    .Include(y => y.Page)
                    .FirstOrDefaultAsync(y => y.Id == yearSectionId.Value);
                if (year == null || year.Page == null) return NotFound();

                ViewData["Title"] = $"{year.Page.Title} — {year.Year}";
                ViewBag.PageId = year.PageId;
                ViewBag.PageTitle = year.Page.Title;
                ViewBag.YearSectionId = (int?)yearSectionId;
                ViewBag.Year = (string?)year.Year;

                var files = await _context.DocumentFiles
                    .Where(f => f.YearSectionId == yearSectionId && !f.IsDeleted)
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync();

                return View(files.Select(f => new DocumentFileVM
                {
                    Id = f.Id,
                    YearSectionId = f.YearSectionId,
                    PageId = year.PageId,
                    Title = f.Title,
                    IsVisible = f.IsVisible,
                    DisplayOrder = f.DisplayOrder,
                    ExistingFilePath = f.FilePath
                }).ToList());
            }

            if (pageId.HasValue)
            {
                var page = await _context.DocumentPages.FindAsync(pageId.Value);
                if (page == null) return NotFound();

                if (page.HasYearSections)
                    return RedirectToAction(nameof(YearSections), new { pageId = page.Id });

                ViewData["Title"] = $"{page.Title} — Files";
                ViewBag.PageId = page.Id;
                ViewBag.PageTitle = page.Title;
                ViewBag.YearSectionId = (int?)null;
                ViewBag.Year = (string?)null;

                var files = await _context.DocumentFiles
                    .Where(f => f.PageId == pageId && f.YearSectionId == null && !f.IsDeleted)
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync();

                return View(files.Select(f => new DocumentFileVM
                {
                    Id = f.Id,
                    YearSectionId = null,
                    PageId = page.Id,
                    Title = f.Title,
                    IsVisible = f.IsVisible,
                    DisplayOrder = f.DisplayOrder,
                    ExistingFilePath = f.FilePath
                }).ToList());
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFile(DocumentFileVM model, IFormFile? DocFile)
        {
            // Year-bound upload
            if (model.YearSectionId.HasValue)
            {
                var year = await _context.DocumentYearSections.FindAsync(model.YearSectionId.Value);
                if (year == null)
                {
                    TempData["Error"] = "Invalid year section.";
                    return RedirectToAction(nameof(Index));
                }
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Title is required.";
                    return RedirectToAction(nameof(Files), new { yearSectionId = model.YearSectionId });
                }

                string? filePath = null;
                if (DocFile != null && DocFile.Length > 0)
                {
                    var uploadResult = await _fileStorage.SaveAsync(DocFile, DocsFolder, FileCategory.Document);
                    if (!uploadResult.Success)
                    {
                        TempData["Error"] = uploadResult.ErrorMessage;
                        return RedirectToAction(nameof(Files), new { yearSectionId = model.YearSectionId });
                    }
                    filePath = uploadResult.RelativePath;
                }

                int maxOrder = await _context.DocumentFiles
                    .Where(f => f.YearSectionId == model.YearSectionId)
                    .Select(f => (int?)f.DisplayOrder).MaxAsync() ?? -1;

                var file = new DocumentFile
                {
                    YearSectionId = model.YearSectionId,
                    Title = model.Title,
                    IsVisible = model.IsVisible,
                    DisplayOrder = maxOrder + 1,
                    FilePath = filePath
                };

                try
                {
                    _context.DocumentFiles.Add(file);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Document added.";
                }
                catch (DbUpdateException ex)
                {
                    _fileStorage.Delete(filePath);
                    _logger.LogError(ex, "Database error adding document file to year section {YearSectionId}", model.YearSectionId);
                    TempData["Error"] = "Unable to add the document. Please try again.";
                }

                return RedirectToAction(nameof(Files), new { yearSectionId = model.YearSectionId });
            }

            // Direct-to-page upload
            var page = await _context.DocumentPages.FindAsync(model.PageId);
            if (page == null)
            {
                TempData["Error"] = "Invalid document page.";
                return RedirectToAction(nameof(Index));
            }
            if (page.HasYearSections)
            {
                TempData["Error"] = "This page requires files to be added under a year section.";
                return RedirectToAction(nameof(YearSections), new { pageId = page.Id });
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(Files), new { pageId = model.PageId });
            }

            string? directFilePath = null;
            if (DocFile != null && DocFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(DocFile, DocsFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    TempData["Error"] = uploadResult.ErrorMessage;
                    return RedirectToAction(nameof(Files), new { pageId = model.PageId });
                }
                directFilePath = uploadResult.RelativePath;
            }

            int maxDirectOrder = await _context.DocumentFiles
                .Where(f => f.PageId == model.PageId && f.YearSectionId == null)
                .Select(f => (int?)f.DisplayOrder).MaxAsync() ?? -1;

            var directFile = new DocumentFile
            {
                PageId = model.PageId,
                Title = model.Title,
                IsVisible = model.IsVisible,
                DisplayOrder = maxDirectOrder + 1,
                FilePath = directFilePath
            };

            try
            {
                _context.DocumentFiles.Add(directFile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Document added.";
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(directFilePath);
                _logger.LogError(ex, "Database error adding document file to page {PageId}", model.PageId);
                TempData["Error"] = "Unable to add the document. Please try again.";
            }

            return RedirectToAction(nameof(Files), new { pageId = model.PageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFileVisible(int id, int? yearSectionId, int? pageId)
        {
            var f = await _context.DocumentFiles.FindAsync(id);
            if (f == null) return NotFound();
            f.IsVisible = !f.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Files), new { yearSectionId, pageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int id, int? yearSectionId, int? pageId)
        {
            var f = await _context.DocumentFiles.FindAsync(id);
            if (f == null) return NotFound();

            string? filePath = f.FilePath;
            f.IsDeleted = true;
            await _context.SaveChangesAsync();
            _fileStorage.Delete(filePath);

            TempData["Success"] = "Document deleted.";
            return RedirectToAction(nameof(Files), new { yearSectionId, pageId });
        }
    }
}