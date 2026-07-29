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

        // ══════════════════ CATEGORIES (top level) ══════════════════

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Documents";
            var categories = await _context.DocumentCategories
                .Include(c => c.YearSections).ThenInclude(y => y.Files)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var vms = categories.Select(c => new DocumentCategoryVM
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible,
                YearCount = c.YearSections.Count(y => !y.IsDeleted),
                FileCount = c.YearSections.SelectMany(y => y.Files).Count(f => !f.IsDeleted)
            }).ToList();

            return View(vms);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Document Category";
            return View(new DocumentCategoryVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentCategoryVM model)
        {
            ViewData["Title"] = "Add Document Category";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.DocumentCategories
                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

            var category = new DocumentCategory
            {
                Title = model.Title,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.DocumentCategories.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Document category {Id} '{Title}' created", category.Id, category.Title);
                TempData["Success"] = "Category added.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating document category '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the category. Please try again.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Document Category";
            var c = await _context.DocumentCategories.FindAsync(id);
            if (c == null) return NotFound();

            return View(new DocumentCategoryVM
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentCategoryVM model)
        {
            ViewData["Title"] = "Edit Document Category";
            if (!ModelState.IsValid) return View(model);

            var c = await _context.DocumentCategories.FindAsync(id);
            if (c == null) return NotFound();

            c.Title = model.Title;
            c.DisplayOrder = model.DisplayOrder;
            c.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating document category {Id}", id);
                ModelState.AddModelError(string.Empty, "Unable to save. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.DocumentCategories.FindAsync(id);
            if (c == null) return NotFound();
            c.IsVisible = !c.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.DocumentCategories
                .Include(x => x.YearSections).ThenInclude(y => y.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            bool hasFiles = c.YearSections.Any(y => y.Files.Any(f => !f.IsDeleted));
            if (hasFiles)
            {
                TempData["Error"] = $"Cannot delete '{c.Title}' — it still has documents. Remove them first.";
                return RedirectToAction(nameof(Index));
            }

            c.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════ YEAR SECTIONS (middle level) ══════════════════

        public async Task<IActionResult> YearSections(int categoryId)
        {
            var category = await _context.DocumentCategories.FindAsync(categoryId);
            if (category == null) return NotFound();

            ViewData["Title"] = $"{category.Title} — Years";
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryTitle = category.Title;

            var years = await _context.DocumentYearSections
                .Include(y => y.Files)
                .Where(y => y.CategoryId == categoryId && !y.IsDeleted)
                .OrderByDescending(y => y.Year)
                .ToListAsync();

            return View(years.Select(y => new DocumentYearSectionVM
            {
                Id = y.Id,
                CategoryId = y.CategoryId,
                Year = y.Year,
                DisplayOrder = y.DisplayOrder,
                FileCount = y.Files.Count(f => !f.IsDeleted)
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddYearSection(DocumentYearSectionVM model)
        {
            if (!await _context.DocumentCategories.AnyAsync(c => c.Id == model.CategoryId))
            {
                TempData["Error"] = "Invalid category.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Year is required.";
                return RedirectToAction(nameof(YearSections), new { categoryId = model.CategoryId });
            }

            int maxOrder = await _context.DocumentYearSections
                .Where(y => y.CategoryId == model.CategoryId)
                .Select(y => (int?)y.DisplayOrder).MaxAsync() ?? -1;

            _context.DocumentYearSections.Add(new DocumentYearSection
            {
                CategoryId = model.CategoryId,
                Year = model.Year,
                DisplayOrder = maxOrder + 1
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Year section added.";
            return RedirectToAction(nameof(YearSections), new { categoryId = model.CategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteYearSection(int id, int categoryId)
        {
            var y = await _context.DocumentYearSections
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (y == null) return NotFound();

            if (y.Files.Any(f => !f.IsDeleted))
            {
                TempData["Error"] = $"Cannot delete '{y.Year}' — it still has files. Remove them first.";
                return RedirectToAction(nameof(YearSections), new { categoryId });
            }

            y.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Year section deleted.";
            return RedirectToAction(nameof(YearSections), new { categoryId });
        }

        // ══════════════════ FILES (leaf level) ══════════════════

        public async Task<IActionResult> Files(int yearSectionId)
        {
            var year = await _context.DocumentYearSections
                .Include(y => y.Category)
                .FirstOrDefaultAsync(y => y.Id == yearSectionId);
            if (year == null) return NotFound();

            ViewData["Title"] = $"{year.Category!.Title} — {year.Year}";
            ViewBag.YearSectionId = yearSectionId;
            ViewBag.CategoryId = year.CategoryId;
            ViewBag.CategoryTitle = year.Category.Title;
            ViewBag.Year = year.Year;

            var files = await _context.DocumentFiles
                .Where(f => f.YearSectionId == yearSectionId && !f.IsDeleted)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            return View(files.Select(f => new DocumentFileVM
            {
                Id = f.Id,
                YearSectionId = f.YearSectionId,
                Title = f.Title,
                IsVisible = f.IsVisible,
                DisplayOrder = f.DisplayOrder,
                ExistingFilePath = f.FilePath
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFile(DocumentFileVM model, IFormFile? DocFile)
        {
            if (!await _context.DocumentYearSections.AnyAsync(y => y.Id == model.YearSectionId))
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFileVisible(int id, int yearSectionId)
        {
            var f = await _context.DocumentFiles.FindAsync(id);
            if (f == null) return NotFound();
            f.IsVisible = !f.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Files), new { yearSectionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int id, int yearSectionId)
        {
            var f = await _context.DocumentFiles.FindAsync(id);
            if (f == null) return NotFound();

            string? filePath = f.FilePath;
            f.IsDeleted = true;
            await _context.SaveChangesAsync();
            _fileStorage.Delete(filePath);

            TempData["Success"] = "Document deleted.";
            return RedirectToAction(nameof(Files), new { yearSectionId });
        }
    }
}
