using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
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
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<SliderController> _logger;
        private const string SlidersFolder = "sliders";

        public SliderController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<SliderController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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

            string? imagePath = null;
            if (SliderImage != null && SliderImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(SliderImage, SlidersFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(SliderImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                imagePath = uploadResult.RelativePath;
            }

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
                IsVisible = model.IsVisible,
                ImagePath = imagePath
            };

            try
            {
                _context.Sliders.Add(slider);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Slider {SliderId} created", slider.Id);

                TempData["Success"] = "Slider added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(imagePath);
                _logger.LogError(ex, "Database error creating slider");
                ModelState.AddModelError(string.Empty, "Unable to save the slider. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(imagePath);
                _logger.LogError(ex, "Unexpected error creating slider");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
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

            string? newImagePath = null;
            bool replacingImage = SliderImage != null && SliderImage.Length > 0;
            if (replacingImage)
            {
                var uploadResult = await _fileStorage.SaveAsync(SliderImage!, SlidersFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(SliderImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newImagePath = uploadResult.RelativePath;
            }

            string? previousImagePath = s.ImagePath;

            s.H3Text = model.H3Text;
            s.H4Text = model.H4Text;
            s.H5Text = model.H5Text;
            s.Anchor1Text = model.Anchor1Text;
            s.Anchor1Link = model.Anchor1Link;
            s.Anchor2Text = model.Anchor2Text;
            s.Anchor2Link = model.Anchor2Link;
            s.IsVisible = model.IsVisible;

            if (replacingImage)
                s.ImagePath = newImagePath;

            try
            {
                await _context.SaveChangesAsync();

                // Old image removed only after the new state is safely persisted.
                if (replacingImage)
                    _fileStorage.Delete(previousImagePath);

                _logger.LogInformation("Slider {SliderId} updated", s.Id);

                TempData["Success"] = "Slider updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingImage)
                    _fileStorage.Delete(newImagePath);

                _logger.LogError(ex, "Database error updating slider {SliderId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the slider. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingImage)
                    _fileStorage.Delete(newImagePath);

                _logger.LogError(ex, "Unexpected error updating slider {SliderId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            s.IsVisible = !s.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Slider {SliderId} visibility set to {IsVisible}", id, s.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for slider {SliderId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            string? imagePath = s.ImagePath;
            s.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Slider {SliderId} deleted", id);
                TempData["Success"] = "Slider deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting slider {SliderId}", id);
                TempData["Error"] = "Unable to delete the slider. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var s = await _context.Sliders.FindAsync(id);
            if (s == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.Sliders.Where(x => x.DisplayOrder == s.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; s.DisplayOrder--; }
            }
            else if (direction == "down")
            {
                var below = await _context.Sliders.Where(x => x.DisplayOrder == s.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; s.DisplayOrder++; }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering slider {SliderId}", id);
                TempData["Error"] = "Unable to reorder sliders. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}