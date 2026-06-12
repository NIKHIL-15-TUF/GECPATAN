using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public GalleryController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? category)
        {
            ViewData["Title"] = "Gallery";
            ViewBag.Category = category;

            var categories = await _context.GalleryImages
                .Where(g => !string.IsNullOrEmpty(g.Category))
                .Select(g => g.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.GalleryImages.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.Category == category);

            var images = await query
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.Id)
                .Select(g => new GalleryImageListVM
                {
                    Id = g.Id,
                    Caption = g.Caption,
                    Category = g.Category,
                    ImagePath = g.ImagePath,
                    IsVisible = g.IsVisible,
                    DisplayOrder = g.DisplayOrder
                })
                .ToListAsync();

            return View(images);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(string? category, string? caption)
        {
            int order = await _context.GalleryImages
                .Select(g => (int?)g.DisplayOrder).MaxAsync() ?? -1;

            foreach (var file in Request.Form.Files)
            {
                if (file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "gallery");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.GalleryImages.Add(new GalleryImage
                    {
                        Caption = string.IsNullOrEmpty(caption)
                                       ? Path.GetFileNameWithoutExtension(file.FileName)
                                       : caption,
                        Category = category,
                        ImagePath = $"/uploads/gallery/{fileName}",
                        IsVisible = true,
                        DisplayOrder = ++order
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Images uploaded.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var g = await _context.GalleryImages.FindAsync(id);
            if (g == null) return NotFound();
            g.IsVisible = !g.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _context.GalleryImages.FindAsync(id);
            if (g == null) return NotFound();

            if (!string.IsNullOrEmpty(g.ImagePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, g.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }

            g.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Image deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}