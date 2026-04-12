using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class MoUController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MoUController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "MoU Documents";
            var items = await _context.MoUDocuments
                .OrderByDescending(m => m.Id)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add MoU";
            return View(new MoUDocumentVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MoUDocumentVM model, IFormFile? MoUFile)
        {
            ViewData["Title"] = "Add MoU";
            if (!ModelState.IsValid) return View(model);

            var mou = new MoUDocument
            {
                Title = model.Title,
                MonthYear = model.MonthYear,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder
            };

            if (MoUFile != null && MoUFile.Length > 0)
                mou.FilePath = await SaveFileAsync(MoUFile, "mou");

            _context.MoUDocuments.Add(mou);
            await _context.SaveChangesAsync();
            TempData["Success"] = "MoU added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit MoU";
            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();
            return View(new MoUDocumentVM
            {
                Id = m.Id,
                Title = m.Title,
                MonthYear = m.MonthYear,
                IsVisible = m.IsVisible,
                DisplayOrder = m.DisplayOrder,
                ExistingFilePath = m.FilePath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MoUDocumentVM model, IFormFile? MoUFile)
        {
            ViewData["Title"] = "Edit MoU";
            if (!ModelState.IsValid) return View(model);

            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();

            m.Title = model.Title;
            m.MonthYear = model.MonthYear;
            m.IsVisible = model.IsVisible;
            m.DisplayOrder = model.DisplayOrder;

            if (MoUFile != null && MoUFile.Length > 0) { DeleteFile(m.FilePath); m.FilePath = await SaveFileAsync(MoUFile, "mou"); }

            await _context.SaveChangesAsync();
            TempData["Success"] = "MoU updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();
            m.IsVisible = !m.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();
            m.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "MoU deleted.";
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
