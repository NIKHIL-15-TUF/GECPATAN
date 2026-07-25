using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class NewsLetterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewsLetterController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "News Letters";
            var items = await _context.NewsLetters
                .OrderByDescending(n => n.Id)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add News Letter";
            return View(new NewsLetterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsLetterVM model, IFormFile? PDF, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Add News Letter";
            if (!ModelState.IsValid) return View(model);

            var nl = new NewsLetter
            {
                Title = model.Title,
                DownloadName = model.DownloadName,
                IsVisible = model.IsVisible
            };

            if (PDF != null && PDF.Length > 0)
                nl.FilePath = await SaveFileAsync(PDF, "newsletters");
            if (Thumbnail != null && Thumbnail.Length > 0)
                nl.ThumbnailPath = await SaveFileAsync(Thumbnail, "newsletters");

            _context.NewsLetters.Add(nl);
            await _context.SaveChangesAsync();
            TempData["Success"] = "News Letter added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit News Letter";
            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();
            return View(new NewsLetterVM
            {
                Id = n.Id,
                Title = n.Title,
                DownloadName = n.DownloadName,
                IsVisible = n.IsVisible,
                ExistingFilePath = n.FilePath,
                ExistingThumbnailPath = n.ThumbnailPath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsLetterVM model, IFormFile? PDF, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Edit News Letter";
            if (!ModelState.IsValid) return View(model);

            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();

            n.Title = model.Title;
            n.DownloadName = model.DownloadName;
            n.IsVisible = model.IsVisible;

            if (PDF != null && PDF.Length > 0) { DeleteFile(n.FilePath); n.FilePath = await SaveFileAsync(PDF, "newsletters"); }
            if (Thumbnail != null && Thumbnail.Length > 0) { DeleteFile(n.ThumbnailPath); n.ThumbnailPath = await SaveFileAsync(Thumbnail, "newsletters"); }

            await _context.SaveChangesAsync();
            TempData["Success"] = "News Letter updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();
            n.IsVisible = !n.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();
            n.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "News Letter deleted.";
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