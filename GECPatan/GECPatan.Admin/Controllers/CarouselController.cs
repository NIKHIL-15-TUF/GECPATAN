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
    public class CarouselController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<CarouselController> _logger;
        private const string CarouselFolder = "carousel";

        private static readonly string[] AllowedVideoHosts =
        {
            "youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be",
            "vimeo.com", "www.vimeo.com", "player.vimeo.com"
        };

        public CarouselController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<CarouselController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        private static string TypeTitle(CarouselType t)
            => t == CarouselType.NotableAlumni ? "Notable Alumni" : "Latest Shots";

        private static bool IsAllowedVideoUrl(string? url)
            => Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u)
               && u.Scheme == Uri.UriSchemeHttps
               && AllowedVideoHosts.Contains(u.Host.ToLowerInvariant());

        private void ValidateForm(CarouselVM m, bool hasNewImage, bool hasExistingImage)
        {
            if (m.IsVideo)
            {
                if (string.IsNullOrWhiteSpace(m.VideoUrl))
                    ModelState.AddModelError(nameof(m.VideoUrl), "Video URL is required.");
                else if (!IsAllowedVideoUrl(m.VideoUrl))
                    ModelState.AddModelError(nameof(m.VideoUrl), "Only https YouTube or Vimeo links are allowed.");
            }
            else if (!hasNewImage && !hasExistingImage)
            {
                ModelState.AddModelError("ImageFile", "An image is required.");
            }
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(CarouselType type = CarouselType.NotableAlumni)
        {
            ViewData["Title"] = TypeTitle(type);
            ViewData["CarouselType"] = type;

            var items = await _context.CarouselItems
                .Where(c => c.Type == type)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Id)
                .ToListAsync();
            return View(items);
        }

        // ── CREATE ────────────────────────────────────────
        public IActionResult Create(CarouselType type = CarouselType.NotableAlumni)
        {
            ViewData["Title"] = "Add " + TypeTitle(type);
            return View("Form", new CarouselVM { Type = type, UploadDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarouselVM model, IFormFile? ImageFile)
        {
            ViewData["Title"] = "Add " + TypeTitle(model.Type);

            bool hasImage = !model.IsVideo && ImageFile != null && ImageFile.Length > 0;
            ValidateForm(model, hasImage, false);
            if (!ModelState.IsValid) return View("Form", model);

            string? imagePath = null;
            if (hasImage)
            {
                var upload = await _fileStorage.SaveAsync(ImageFile!, CarouselFolder, FileCategory.Image);
                if (!upload.Success)
                {
                    ModelState.AddModelError("ImageFile", upload.ErrorMessage!);
                    return View("Form", model);
                }
                imagePath = upload.RelativePath;
            }

            int maxOrder = await _context.CarouselItems
                .Where(c => c.Type == model.Type)
                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

            var item = new CarouselItem
            {
                Type = model.Type,
                Title = model.Title,
                ImagePath = imagePath,
                IsVideo = model.IsVideo,
                VideoUrl = model.IsVideo ? model.VideoUrl!.Trim() : null,
                UploadDate = model.UploadDate,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.CarouselItems.Add(item);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Carousel item {Id} ({Type}) created", item.Id, item.Type);
                TempData["Success"] = "Item added.";
                return RedirectToAction(nameof(Index), new { type = item.Type });
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(imagePath);
                _logger.LogError(ex, "Error creating carousel item");
                ModelState.AddModelError(string.Empty, "Unable to save the item. Please try again.");
                return View("Form", model);
            }
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _context.CarouselItems.FindAsync(id);
            if (c == null) return NotFound();

            ViewData["Title"] = "Edit " + TypeTitle(c.Type);
            return View("Form", new CarouselVM
            {
                Id = c.Id,
                Type = c.Type,
                Title = c.Title,
                ExistingImagePath = c.ImagePath,
                IsVideo = c.IsVideo,
                VideoUrl = c.VideoUrl,
                UploadDate = c.UploadDate,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CarouselVM model, IFormFile? ImageFile)
        {
            var item = await _context.CarouselItems.FindAsync(id);
            if (item == null) return NotFound();

            model.Id = id;
            model.Type = item.Type;                       // never trust the posted type
            model.ExistingImagePath = item.ImagePath;
            ViewData["Title"] = "Edit " + TypeTitle(item.Type);

            bool newImage = !model.IsVideo && ImageFile != null && ImageFile.Length > 0;
            ValidateForm(model, newImage, !string.IsNullOrEmpty(item.ImagePath));
            if (!ModelState.IsValid) return View("Form", model);

            string? newPath = null;
            if (newImage)
            {
                var upload = await _fileStorage.SaveAsync(ImageFile!, CarouselFolder, FileCategory.Image);
                if (!upload.Success)
                {
                    ModelState.AddModelError("ImageFile", upload.ErrorMessage!);
                    return View("Form", model);
                }
                newPath = upload.RelativePath;
            }

            string? previousPath = item.ImagePath;

            item.Title = model.Title;
            item.IsVideo = model.IsVideo;
            item.VideoUrl = model.IsVideo ? model.VideoUrl!.Trim() : null;
            item.UploadDate = model.UploadDate;
            item.IsVisible = model.IsVisible;

            if (newImage) item.ImagePath = newPath;
            else if (model.IsVideo) item.ImagePath = null;

            try
            {
                await _context.SaveChangesAsync();

                if (newImage || (model.IsVideo && previousPath != null))
                    _fileStorage.Delete(previousPath);

                _logger.LogInformation("Carousel item {Id} updated", id);
                TempData["Success"] = "Item updated.";
                return RedirectToAction(nameof(Index), new { type = item.Type });
            }
            catch (Exception ex)
            {
                if (newImage) _fileStorage.Delete(newPath);
                _logger.LogError(ex, "Error updating carousel item {Id}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the item. Please try again.");
                return View("Form", model);
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.CarouselItems.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVisible = !item.IsVisible;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error toggling carousel item {Id}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }
            return RedirectToAction(nameof(Index), new { type = item.Type });
        }

        // ── DELETE (soft) ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CarouselItems.FindAsync(id);
            if (item == null) return NotFound();

            string? path = item.ImagePath;
            item.IsDeleted = true;
            try
            {
                await _context.SaveChangesAsync();
                _fileStorage.Delete(path);
                TempData["Success"] = "Item deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting carousel item {Id}", id);
                TempData["Error"] = "Unable to delete the item. Please try again.";
            }
            return RedirectToAction(nameof(Index), new { type = item.Type });
        }

        // ── REORDER (within the same carousel type) ───────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var item = await _context.CarouselItems.FindAsync(id);
            if (item == null) return NotFound();

            var all = await _context.CarouselItems
                .Where(c => c.Type == item.Type)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id)
                .ToListAsync();

            int idx = all.FindIndex(c => c.Id == id);
            int other = direction == "up" ? idx - 1 : direction == "down" ? idx + 1 : -1;

            if (other >= 0 && other < all.Count)
            {
                (all[idx].DisplayOrder, all[other].DisplayOrder) = (all[other].DisplayOrder, all[idx].DisplayOrder);
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error reordering carousel item {Id}", id);
                    TempData["Error"] = "Unable to reorder items. Please try again.";
                }
            }
            return RedirectToAction(nameof(Index), new { type = item.Type });
        }
    }
}
