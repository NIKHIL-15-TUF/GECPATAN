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
    public class NewsLetterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<NewsLetterController> _logger;
        private const string NewslettersFolder = "newsletters";

        public NewsLetterController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<NewsLetterController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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

            var savedPaths = new List<string>();

            string? filePath = null;
            if (PDF != null && PDF.Length > 0)
            {
                var result = await _fileStorage.SaveAsync(PDF, NewslettersFolder, FileCategory.Document);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(PDF), result.ErrorMessage!);
                    return View(model);
                }
                filePath = result.RelativePath;
                savedPaths.Add(filePath!);
            }

            string? thumbnailPath = null;
            if (Thumbnail != null && Thumbnail.Length > 0)
            {
                var result = await _fileStorage.SaveAsync(Thumbnail, NewslettersFolder, FileCategory.Image);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(nameof(Thumbnail), result.ErrorMessage!);
                    return View(model);
                }
                thumbnailPath = result.RelativePath;
                savedPaths.Add(thumbnailPath!);
            }

            var newsLetter = new NewsLetter
            {
                Title = model.Title,
                DownloadName = model.DownloadName,
                IsVisible = model.IsVisible,
                FilePath = filePath,
                ThumbnailPath = thumbnailPath
            };

            try
            {
                _context.NewsLetters.Add(newsLetter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Newsletter {NewsLetterId} '{Title}' created", newsLetter.Id, newsLetter.Title);

                TempData["Success"] = "News Letter added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error creating newsletter '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the newsletter. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error creating newsletter '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
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

            var savedPaths = new List<string>();

            bool replacingFile = PDF != null && PDF.Length > 0;
            string? newFilePath = null;
            if (replacingFile)
            {
                var result = await _fileStorage.SaveAsync(PDF!, NewslettersFolder, FileCategory.Document);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(PDF), result.ErrorMessage!);
                    return View(model);
                }
                newFilePath = result.RelativePath;
                savedPaths.Add(newFilePath!);
            }

            bool replacingThumbnail = Thumbnail != null && Thumbnail.Length > 0;
            string? newThumbnailPath = null;
            if (replacingThumbnail)
            {
                var result = await _fileStorage.SaveAsync(Thumbnail!, NewslettersFolder, FileCategory.Image);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(nameof(Thumbnail), result.ErrorMessage!);
                    return View(model);
                }
                newThumbnailPath = result.RelativePath;
                savedPaths.Add(newThumbnailPath!);
            }

            string? previousFilePath = n.FilePath;
            string? previousThumbnailPath = n.ThumbnailPath;

            n.Title = model.Title;
            n.DownloadName = model.DownloadName;
            n.IsVisible = model.IsVisible;

            if (replacingFile) n.FilePath = newFilePath;
            if (replacingThumbnail) n.ThumbnailPath = newThumbnailPath;

            try
            {
                await _context.SaveChangesAsync();

                // Old files removed only after the new state is safely persisted.
                if (replacingFile) _fileStorage.Delete(previousFilePath);
                if (replacingThumbnail) _fileStorage.Delete(previousThumbnailPath);

                _logger.LogInformation("Newsletter {NewsLetterId} '{Title}' updated", n.Id, n.Title);

                TempData["Success"] = "News Letter updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error updating newsletter {NewsLetterId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the newsletter. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error updating newsletter {NewsLetterId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();

            n.IsVisible = !n.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Newsletter {NewsLetterId} visibility set to {IsVisible}", id, n.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for newsletter {NewsLetterId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.NewsLetters.FindAsync(id);
            if (n == null) return NotFound();

            string? filePath = n.FilePath;
            string? thumbnailPath = n.ThumbnailPath;
            n.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical files removed only after the soft-delete commits.
                _fileStorage.Delete(filePath);
                _fileStorage.Delete(thumbnailPath);

                _logger.LogInformation("Newsletter {NewsLetterId} '{Title}' deleted", id, n.Title);
                TempData["Success"] = "News Letter deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting newsletter {NewsLetterId}", id);
                TempData["Error"] = "Unable to delete the newsletter. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}