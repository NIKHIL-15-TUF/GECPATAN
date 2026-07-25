using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class SliderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SliderController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Home Sliders";
            var sliders = await _context.Sliders
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
            return View(sliders);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Slider";
            return View(new SliderVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SliderVM model, IFormFile? SliderImage)
        {
            ViewData["Title"] = "Add Slider";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.Sliders.Select(s => (int?)s.DisplayOrder).MaxAsync() ?? -1;

            var slider = new Slider
            {
                H3Text = model.H3Text,
                H4Text = model.H4Text,
                H5Text = model.H5Text,
                Anchor1Text = model.Anchor1Text,
                Anchor1Link = model.Anchor1Link,
                Anchor2Text = model.Anchor2Text,
                Anchor2Link = model.Anchor2Link,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            if (SliderImage != null && SliderImage.Length > 0)
                slider.ImagePath = await SaveFileAsync(SliderImage, "sliders");

            _context.Sliders.Add(slider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Slider added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Slider";
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            return View(new SliderVM
            {
                Id = s.Id,
                H3Text = s.H3Text,
                H4Text = s.H4Text,
                H5Text = s.H5Text,
                Anchor1Text = s.Anchor1Text,
                Anchor1Link = s.Anchor1Link,
                Anchor2Text = s.Anchor2Text,
                Anchor2Link = s.Anchor2Link,
                DisplayOrder = s.DisplayOrder,
                IsVisible = s.IsVisible,
                ExistingImagePath = s.ImagePath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SliderVM model, IFormFile? SliderImage)
        {
            ViewData["Title"] = "Edit Slider";
            if (!ModelState.IsValid) return View(model);

            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            s.H3Text = model.H3Text;
            s.H4Text = model.H4Text;
            s.H5Text = model.H5Text;
            s.Anchor1Text = model.Anchor1Text;
            s.Anchor1Link = model.Anchor1Link;
            s.Anchor2Text = model.Anchor2Text;
            s.Anchor2Link = model.Anchor2Link;
            s.IsVisible = model.IsVisible;

            if (SliderImage != null && SliderImage.Length > 0)
            {
                DeleteFile(s.ImagePath);
                s.ImagePath = await SaveFileAsync(SliderImage, "sliders");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Slider updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();
            s.IsVisible = !s.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();
            DeleteFile(s.ImagePath);
            s.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Slider deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.Sliders.Where(x => x.DisplayOrder == s.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; s.DisplayOrder--; }
            }
            else
            {
                var below = await _context.Sliders.Where(x => x.DisplayOrder == s.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; s.DisplayOrder++; }
            }

            await _context.SaveChangesAsync();
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