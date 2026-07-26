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
    public class MoUController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<MoUController> _logger;
        private const string MoUFolder = "mou";

        public MoUController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<MoUController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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

            string? filePath = null;
            bool hasFile = MoUFile != null && MoUFile.Length > 0;
            if (hasFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(MoUFile!, MoUFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(MoUFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                filePath = uploadResult.RelativePath;
            }

            var mou = new MoUDocument
            {
                Title = model.Title,
                MonthYear = model.MonthYear,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder,
                FilePath = filePath
            };

            try
            {
                _context.MoUDocuments.Add(mou);
                await _context.SaveChangesAsync();

                _logger.LogInformation("MoU document {MoUId} '{Title}' created", mou.Id, mou.Title);

                TempData["Success"] = "MoU added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Database error creating MoU '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the MoU. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Unexpected error creating MoU '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
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

            string? newFilePath = null;
            bool replacingFile = MoUFile != null && MoUFile.Length > 0;
            if (replacingFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(MoUFile!, MoUFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(MoUFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newFilePath = uploadResult.RelativePath;
            }

            string? previousFilePath = m.FilePath;

            m.Title = model.Title;
            m.MonthYear = model.MonthYear;
            m.IsVisible = model.IsVisible;
            m.DisplayOrder = model.DisplayOrder;

            if (replacingFile)
                m.FilePath = newFilePath;

            try
            {
                await _context.SaveChangesAsync();

                // Old file removed only after the new state is safely persisted.
                if (replacingFile)
                    _fileStorage.Delete(previousFilePath);

                _logger.LogInformation("MoU document {MoUId} '{Title}' updated", m.Id, m.Title);

                TempData["Success"] = "MoU updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Database error updating MoU {MoUId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the MoU. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Unexpected error updating MoU {MoUId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();

            m.IsVisible = !m.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("MoU document {MoUId} visibility set to {IsVisible}", id, m.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for MoU {MoUId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.MoUDocuments.FindAsync(id);
            if (m == null) return NotFound();

            string? filePath = m.FilePath;
            m.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(filePath);

                _logger.LogInformation("MoU document {MoUId} '{Title}' deleted", id, m.Title);
                TempData["Success"] = "MoU deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting MoU {MoUId}", id);
                TempData["Error"] = "Unable to delete the MoU. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}