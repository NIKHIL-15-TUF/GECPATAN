using GECPatan.Core.Data;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class TenderCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly NotificationService _notify;
        private readonly ILogger<TenderCategoryController> _logger;
        private const string TendersFolder = "tenders";

        public TenderCategoryController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            NotificationService notify,
            ILogger<TenderCategoryController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _notify = notify;
            _logger = logger;
        }

        // ── INDEX: list all categories ────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Tenders";
            var categories = await _context.TenderCategories
                .Include(t => t.Documents)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var vms = categories.Select(c => new TenderCategoryVM
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible,
                Documents = c.Documents
                    .Where(d => !d.IsDeleted)
                    .Select(d => new TenderDocumentVM
                    {
                        Id = d.Id,
                        TenderCategoryId = d.TenderCategoryId,
                        DocTitle = d.DocTitle,
                        ValidFrom = d.ValidFrom,
                        ValidTo = d.ValidTo,
                        IsVisible = d.IsVisible,
                        ExistingFilePath = d.FilePath
                    }).ToList()
            }).ToList();

            return View(vms);
        }

        // ── CREATE CATEGORY ───────────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Tender Category";
            return View(new TenderCategoryVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenderCategoryVM model)
        {
            ViewData["Title"] = "Add Tender Category";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.TenderCategories
                .Select(t => (int?)t.DisplayOrder).MaxAsync() ?? -1;

            var category = new TenderCategory
            {
                Title = model.Title,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.TenderCategories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tender category {CategoryId} '{Title}' created", category.Id, category.Title);

                TempData["Success"] = "Tender category added.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating tender category '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the category. Please try again.");
                return View(model);
            }

            try
            {
                await _notify.SendAsync(
                    title: $"New Tender Category: {category.Title}",
                    module: "Tender",
                    icon: "fa-file-contract",
                    color: "warning",
                    link: "/TenderCategory/Index",
                    forRole: "SuperAdmin"
                );
            }
            catch (Exception ex)
            {
                // A failed notification shouldn't undo or block an already-saved category.
                _logger.LogError(ex, "Failed to send notification for tender category {CategoryId}", category.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        // ── EDIT CATEGORY ─────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Tender Category";
            var c = await _context.TenderCategories.FindAsync(id);
            if (c == null) return NotFound();

            return View(new TenderCategoryVM
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TenderCategoryVM model)
        {
            ViewData["Title"] = "Edit Tender Category";
            if (!ModelState.IsValid) return View(model);

            var c = await _context.TenderCategories.FindAsync(id);
            if (c == null) return NotFound();

            c.Title = model.Title;
            c.DisplayOrder = model.DisplayOrder;
            c.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tender category {CategoryId} updated", c.Id);

                TempData["Success"] = "Category updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating tender category {CategoryId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the category. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.TenderCategories.FindAsync(id);
            if (c == null) return NotFound();

            c.IsVisible = !c.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tender category {CategoryId} visibility set to {IsVisible}", id, c.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for tender category {CategoryId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.TenderCategories
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            bool hasDocuments = c.Documents.Any(d => !d.IsDeleted);
            if (hasDocuments)
            {
                TempData["Error"] = $"Cannot delete '{c.Title}' — it still has tender documents. Remove them first.";
                return RedirectToAction(nameof(Index));
            }

            c.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tender category {CategoryId} '{Title}' deleted", id, c.Title);
                TempData["Success"] = "Category deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting tender category {CategoryId}", id);
                TempData["Error"] = "Unable to delete the category. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // TENDER DOCUMENTS (child)
        // ══════════════════════════════════════════════════

        public async Task<IActionResult> Documents(int categoryId)
        {
            ViewData["Title"] = "Tender Documents";
            var category = await _context.TenderCategories.FindAsync(categoryId);
            if (category == null) return NotFound();

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryTitle = category.Title;

            var docs = await _context.TenderDocuments
                .Where(d => d.TenderCategoryId == categoryId && !d.IsDeleted)
                .OrderByDescending(d => d.ValidFrom)
                .ToListAsync();

            return View(docs.Select(d => new TenderDocumentVM
            {
                Id = d.Id,
                TenderCategoryId = d.TenderCategoryId,
                DocTitle = d.DocTitle,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo,
                MonthYear = d.MonthYear,
                IsVisible = d.IsVisible,
                ExistingFilePath = d.FilePath,
                CategoryTitle = category.Title
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocument(TenderDocumentVM model, IFormFile? DocFile)
        {
            if (!await _context.TenderCategories.AnyAsync(c => c.Id == model.TenderCategoryId))
            {
                TempData["Error"] = "Please select a valid tender category.";
                return RedirectToAction(nameof(Documents), new { categoryId = model.TenderCategoryId });
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(Documents), new { categoryId = model.TenderCategoryId });
            }

            string? filePath = null;
            if (DocFile != null && DocFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(DocFile, TendersFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    TempData["Error"] = uploadResult.ErrorMessage;
                    return RedirectToAction(nameof(Documents), new { categoryId = model.TenderCategoryId });
                }
                filePath = uploadResult.RelativePath;
            }

            var doc = new TenderDocument
            {
                TenderCategoryId = model.TenderCategoryId,
                DocTitle = model.DocTitle,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                MonthYear = model.MonthYear,
                IsVisible = model.IsVisible,
                FilePath = filePath
            };

            try
            {
                _context.TenderDocuments.Add(doc);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tender document {DocId} '{DocTitle}' added to category {CategoryId}",
                    doc.Id, doc.DocTitle, model.TenderCategoryId);

                TempData["Success"] = "Document added.";
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Database error adding tender document to category {CategoryId}", model.TenderCategoryId);
                TempData["Error"] = "Unable to add the document. Please try again.";
            }

            return RedirectToAction(nameof(Documents), new { categoryId = model.TenderCategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDocVisible(int id, int categoryId)
        {
            var d = await _context.TenderDocuments.FindAsync(id);
            if (d == null) return NotFound();

            d.IsVisible = !d.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tender document {DocId} visibility set to {IsVisible}", id, d.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for tender document {DocId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Documents), new { categoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int categoryId)
        {
            var d = await _context.TenderDocuments.FindAsync(id);
            if (d == null) return NotFound();

            string? filePath = d.FilePath;
            d.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(filePath);

                _logger.LogInformation("Tender document {DocId} deleted from category {CategoryId}", id, categoryId);
                TempData["Success"] = "Document deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting tender document {DocId}", id);
                TempData["Error"] = "Unable to delete the document. Please try again.";
            }

            return RedirectToAction(nameof(Documents), new { categoryId });
        }
    }
}