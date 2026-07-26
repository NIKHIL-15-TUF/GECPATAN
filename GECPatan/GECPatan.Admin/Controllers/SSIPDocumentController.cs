using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
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
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<SSIPDocumentController> _logger;
        private const string SSIPFolder = "ssip";

        public SSIPDocumentController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<SSIPDocumentController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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

            string? filePath = null;
            if (DocFile != null && DocFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(DocFile, SSIPFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(DocFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                filePath = uploadResult.RelativePath;
            }

            int maxOrder = await _context.SSIPDocuments
                .Select(s => (int?)s.DisplayOrder).MaxAsync() ?? -1;

            var doc = new SSIPDocument
            {
                Title = model.Title,
                UploadDate = model.UploadDate,
                IsVisible = model.IsVisible,
                DisplayOrder = maxOrder + 1,
                FilePath = filePath
            };

            try
            {
                _context.SSIPDocuments.Add(doc);
                await _context.SaveChangesAsync();

                _logger.LogInformation("SSIP document {DocId} '{Title}' created", doc.Id, doc.Title);

                TempData["Success"] = "SSIP Document added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Database error creating SSIP document '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the document. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Unexpected error creating SSIP document '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
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

            string? newFilePath = null;
            bool replacingFile = DocFile != null && DocFile.Length > 0;
            if (replacingFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(DocFile!, SSIPFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(DocFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newFilePath = uploadResult.RelativePath;
            }

            string? previousFilePath = s.FilePath;

            s.Title = model.Title;
            s.UploadDate = model.UploadDate;
            s.IsVisible = model.IsVisible;

            if (replacingFile)
                s.FilePath = newFilePath;

            try
            {
                await _context.SaveChangesAsync();

                // Old file removed only after the new state is safely persisted.
                if (replacingFile)
                    _fileStorage.Delete(previousFilePath);

                _logger.LogInformation("SSIP document {DocId} '{Title}' updated", s.Id, s.Title);

                TempData["Success"] = "SSIP Document updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Database error updating SSIP document {DocId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the document. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Unexpected error updating SSIP document {DocId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();

            s.IsVisible = !s.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("SSIP document {DocId} visibility set to {IsVisible}", id, s.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for SSIP document {DocId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.SSIPDocuments.FindAsync(id);
            if (s == null) return NotFound();

            string? filePath = s.FilePath;
            s.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(filePath);

                _logger.LogInformation("SSIP document {DocId} '{Title}' deleted", id, s.Title);
                TempData["Success"] = "Deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting SSIP document {DocId}", id);
                TempData["Error"] = "Unable to delete the document. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}