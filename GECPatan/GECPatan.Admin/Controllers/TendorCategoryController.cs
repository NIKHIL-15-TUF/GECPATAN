using GECPatan.Core.Data;
using GECPatan.Core.Services;
using  GECPatan.Core.Models.Domain;
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
        private readonly IWebHostEnvironment _env;
        private readonly NotificationService _notify;
        public TenderCategoryController(ApplicationDbContext context, IWebHostEnvironment env , NotificationService notify)
        {
            _context = context;
            _env = env;
            _notify = notify;
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

            _context.TenderCategories.Add(new TenderCategory
            {
                Title = model.Title,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tender category added.";
            //Notification
            await _notify.SendAsync(
                title: $"New Tender Category: {model.Title}",
                module: "Tender",
                icon: "fa-file-contract",
                color: "warning",
                link: "/TenderCategory/Index",
                forRole: "SuperAdmin"
            );
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

            await _context.SaveChangesAsync();
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.TenderCategories.FindAsync(id);
            if (c == null) return NotFound();
            c.IsVisible = !c.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.TenderCategories.FindAsync(id);
            if (c == null) return NotFound();
            c.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
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
                .Where(d => d.TenderCategoryId == categoryId)
                .OrderByDescending(d => d.ValidFrom)
                .ToListAsync();

            return View(docs.Select(d => new TenderDocumentVM
            {
                Id = d.Id,
                TenderCategoryId = d.TenderCategoryId,
                DocTitle = d.DocTitle,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo,
                MonthYear= d.MonthYear,
                IsVisible = d.IsVisible,
                ExistingFilePath = d.FilePath,
                CategoryTitle = category.Title
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocument(TenderDocumentVM model, IFormFile? DocFile)
        {
            if (ModelState.IsValid)
            {
                var doc = new TenderDocument
                {
                    TenderCategoryId = model.TenderCategoryId,
                    DocTitle = model.DocTitle,
                    ValidFrom = model.ValidFrom,
                    ValidTo = model.ValidTo,
                    MonthYear = model.MonthYear,
                    IsVisible = model.IsVisible
                };

                if (DocFile != null && DocFile.Length > 0)
                    doc.FilePath = await SaveFileAsync(DocFile, "tenders");

                _context.TenderDocuments.Add(doc);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Document added.";
            }
            return RedirectToAction(nameof(Documents), new { categoryId = model.TenderCategoryId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDocVisible(int id, int categoryId)
        {
            var d = await _context.TenderDocuments.FindAsync(id);
            if (d == null) return NotFound();
            d.IsVisible = !d.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Documents), new { categoryId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id, int categoryId)
        {
            var d = await _context.TenderDocuments.FindAsync(id);
            if (d == null) return NotFound();
            DeleteFile(d.FilePath);
            d.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Document deleted.";
            return RedirectToAction(nameof(Documents), new { categoryId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}