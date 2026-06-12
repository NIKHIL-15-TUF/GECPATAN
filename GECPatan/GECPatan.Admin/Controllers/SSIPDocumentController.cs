using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,HOD")]
    public class SSIPDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SSIPDocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "SSIP Documents";
            var items = await _context.SSIPDocuments
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add SSIP Document";
            return View(new SSIPDocumentVM
            {
                UploadDate = DateTime.Today.ToString("dd MMM yyyy")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SSIPDocumentVM model, IFormFile? DocFile)
        {
            ViewData["Title"] = "Add SSIP Document";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.SSIPDocuments
                .Select(s => (int?)s.DisplayOrder).MaxAsync() ?? -1;

            var doc = new SSIPDocument
            {
                Title = model.Title,
                UploadDate = model.UploadDate,
                IsVisible = model.IsVisible,
                DisplayOrder = maxOrder + 1
            };

            if (DocFile != null && DocFile.Length > 0)
                doc.FilePath = await SaveFileAsync(DocFile, "ssip");

            _context.SSIPDocuments.Add(doc);
            await _context.SaveChangesAsync();
            TempData["Success"] = "SSIP Document added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit SSIP Document";
            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();
            return View(new SSIPDocumentVM
            {
                Id = s.Id,
                Title = s.Title,
                UploadDate = s.UploadDate,
                IsVisible = s.IsVisible,
                DisplayOrder = s.DisplayOrder,
                ExistingFilePath = s.FilePath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SSIPDocumentVM model, IFormFile? DocFile)
        {
            ViewData["Title"] = "Edit SSIP Document";
            if (!ModelState.IsValid) return View(model);

            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();

            s.Title = model.Title;
            s.UploadDate = model.UploadDate;
            s.IsVisible = model.IsVisible;

            if (DocFile != null && DocFile.Length > 0)
            {
                DeleteFile(s.FilePath);
                s.FilePath = await SaveFileAsync(DocFile, "ssip");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "SSIP Document updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();
            s.IsVisible = !s.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();
            DeleteFile(s.FilePath);
            s.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted.";
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