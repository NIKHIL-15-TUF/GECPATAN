using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class ImportantDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ImportantDocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Important Documents";
            var items = await _context.ImportantDocuments
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Document";
            return View(new ImportantDocumentVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ImportantDocumentVM model, IFormFile? DocFile)
        {
            ViewData["Title"] = "Add Document";
            if (!ModelState.IsValid) return View(model);

            var doc = new ImportantDocument
            {
                Title = model.Title,
                FileType = model.FileType,
                UploadDate = model.UploadDate,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder
            };

            if (DocFile != null && DocFile.Length > 0)
                doc.FilePath = await SaveFileAsync(DocFile, "documents");

            _context.ImportantDocuments.Add(doc);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Document added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Document";
            var d = await _context.ImportantDocuments.FindAsync(id);
            if (d == null) return NotFound();
            return View(new ImportantDocumentVM
            {
                Id = d.Id,
                Title = d.Title,
                FileType = d.FileType,
                UploadDate = d.UploadDate,
                IsVisible = d.IsVisible,
                DisplayOrder = d.DisplayOrder,
                ExistingFilePath = d.FilePath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ImportantDocumentVM model, IFormFile? DocFile)
        {
            ViewData["Title"] = "Edit Document";
            if (!ModelState.IsValid) return View(model);

            var d = await _context.ImportantDocuments.FindAsync(id);
            if (d == null) return NotFound();

            d.Title = model.Title;
            d.FileType = model.FileType;
            d.UploadDate = model.UploadDate;
            d.IsVisible = model.IsVisible;
            d.DisplayOrder = model.DisplayOrder;

            if (DocFile != null && DocFile.Length > 0) { DeleteFile(d.FilePath); d.FilePath = await SaveFileAsync(DocFile, "documents"); }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Document updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var d = await _context.ImportantDocuments.FindAsync(id);
            if (d == null) return NotFound();
            d.IsVisible = !d.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var d = await _context.ImportantDocuments.FindAsync(id);
            if (d == null) return NotFound();
            d.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Document deleted.";
            return RedirectToAction(nameof(Index));
        }

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
