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
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<GalleryController> _logger;
        private const string GalleryFolder = "gallery";

        public GalleryController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<GalleryController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string? category, string? caption)
        {
            if (Request.Form.Files.Count == 0)
            {
                TempData["Error"] = "Please choose at least one image to upload.";
                return RedirectToAction(nameof(Index));
            }

            // Validate & save every file first. If any file fails validation,
            // discard everything saved so far in this batch rather than
            // uploading a partial set with a silent skip.
            var savedPaths = new List<string>();
            var newImages = new List<GalleryImage>();

            int order = await _context.GalleryImages
                .Select(g => (int?)g.DisplayOrder).MaxAsync() ?? -1;

            foreach (var file in Request.Form.Files)
            {
                if (file.Length == 0) continue;

                var uploadResult = await _fileStorage.SaveAsync(file, GalleryFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);

                    _logger.LogWarning(
                        "Gallery upload rejected file '{FileName}': {Reason}",
                        file.FileName, uploadResult.ErrorMessage);

                    TempData["Error"] = $"'{file.FileName}': {uploadResult.ErrorMessage}";
                    return RedirectToAction(nameof(Index));
                }

                savedPaths.Add(uploadResult.RelativePath!);
                newImages.Add(new GalleryImage
                {
                    Caption = string.IsNullOrEmpty(caption)
                        ? Path.GetFileNameWithoutExtension(file.FileName)
                        : caption,
                    Category = category,
                    ImagePath = uploadResult.RelativePath,
                    IsVisible = true,
                    DisplayOrder = ++order
                });
            }

            try
            {
                _context.GalleryImages.AddRange(newImages);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} gallery image(s) uploaded to category '{Category}'",
                    newImages.Count, category ?? "(none)");

                TempData["Success"] = $"{newImages.Count} image(s) uploaded.";
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error saving {Count} uploaded gallery image(s)", newImages.Count);
                TempData["Error"] = "Unable to save the uploaded images. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var g = await _context.GalleryImages.FindAsync(id);
            if (g == null) return NotFound();

            g.IsVisible = !g.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Gallery image {ImageId} visibility set to {IsVisible}", id, g.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for gallery image {ImageId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _context.GalleryImages.FindAsync(id);
            if (g == null) return NotFound();

            string? imagePath = g.ImagePath;
            g.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Gallery image {ImageId} deleted", id);
                TempData["Success"] = "Image deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting gallery image {ImageId}", id);
                TempData["Error"] = "Unable to delete the image. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}