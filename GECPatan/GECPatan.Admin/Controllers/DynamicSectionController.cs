using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class DynamicSectionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DynamicSectionController> _logger;
        private const string SectionsFolder = "sections";

        public DynamicSectionController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<DynamicSectionController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX: list all sections for a page ───────────
        public async Task<IActionResult> Index(PageType pageType, int pageId)
        {
            ViewData["Title"] = "Manage Sections";
            ViewBag.PageType = pageType;
            ViewBag.PageId = pageId;

            string pageName = await GetPageNameAsync(pageType, pageId);
            ViewBag.PageName = pageName;

            var sections = await _context.DynamicSections
                .Where(s => s.PageType == pageType && s.PageId == pageId)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new DynamicSectionListVM
                {
                    Id = s.Id,
                    Title = s.Title,
                    SectionType = s.SectionType,
                    SectionTypeName = s.SectionType.ToString(),
                    DisplayOrder = s.DisplayOrder,
                    IsVisible = s.IsVisible,
                    PageType = s.PageType,
                    PageId = s.PageId,
                    PageName = pageName
                })
                .ToListAsync();

            return View(sections);
        }

        // ── CREATE GET ────────────────────────────────────
        public IActionResult Create(PageType pageType, int pageId)
        {
            ViewData["Title"] = "Add Section";
            ViewBag.PageType = pageType;
            ViewBag.PageId = pageId;

            var vm = new DynamicSectionVM
            {
                PageType = pageType,
                PageId = pageId,
                SectionTypes = GetSectionTypeList()
            };
            return View(vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DynamicSectionVM model, IFormFile? UploadFile)
        {
            ViewData["Title"] = "Add Section";
            model.SectionTypes = GetSectionTypeList();
            ViewBag.PageType = model.PageType;
            ViewBag.PageId = model.PageId;

            if (!ModelState.IsValid)
                return View(model);

            bool isGalleryOrFileList = model.SectionType == SectionType.ImageGallery ||
                                        model.SectionType == SectionType.FileList;
            bool isPdfType = model.SectionType == SectionType.PDFViewer ||
                              model.SectionType == SectionType.PDFDownload;

            // Validate & save all files up front, before touching the database,
            // so we never end up with a half-saved section.
            var savedPaths = new List<string>();

            string? sectionFilePath = null;
            if (UploadFile != null && UploadFile.Length > 0 && isPdfType)
            {
                var result = await _fileStorage.SaveAsync(UploadFile, SectionsFolder, FileCategory.Document);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(UploadFile), result.ErrorMessage!);
                    return View(model);
                }
                sectionFilePath = result.RelativePath!;
                savedPaths.Add(sectionFilePath);
            }

            var galleryFiles = new List<(string Path, string Title, string FileType)>();
            if (isGalleryOrFileList && Request.Form.Files.Count > 0)
            {
                var category = model.SectionType == SectionType.ImageGallery
                    ? FileCategory.Image : FileCategory.Document;
                string fileType = model.SectionType == SectionType.ImageGallery ? "Image" : "PDF";

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length == 0) continue;

                    var result = await _fileStorage.SaveAsync(file, SectionsFolder, category);
                    if (!result.Success)
                    {
                        // Abort and clean up everything already saved in this request.
                        foreach (var path in savedPaths) _fileStorage.Delete(path);
                        ModelState.AddModelError(string.Empty,
                            $"'{file.FileName}': {result.ErrorMessage}");
                        return View(model);
                    }

                    savedPaths.Add(result.RelativePath!);
                    galleryFiles.Add((result.RelativePath!, Path.GetFileNameWithoutExtension(file.FileName), fileType));
                }
            }

            int maxOrder = await _context.DynamicSections
                .Where(s => s.PageType == model.PageType && s.PageId == model.PageId)
                .Select(s => (int?)s.DisplayOrder)
                .MaxAsync() ?? -1;

            var section = new DynamicSection
            {
                Title = model.Title,
                SectionType = model.SectionType,
                HtmlContent = model.HtmlContent,
                PageType = model.PageType,
                PageId = model.PageId,
                DisplayOrder = maxOrder + 1,
                IsVisible = true,
                FilePath = sectionFilePath,
                FileName = sectionFilePath != null ? UploadFile!.FileName : null
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.DynamicSections.Add(section);
                await _context.SaveChangesAsync();

                foreach (var (path, title, fileType) in galleryFiles)
                {
                    _context.DynamicSectionFiles.Add(new DynamicSectionFile
                    {
                        DynamicSectionId = section.Id,
                        FilePath = path,
                        Title = title,
                        FileType = fileType,
                        DisplayOrder = galleryFiles.IndexOf((path, title, fileType))
                    });
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Section {SectionId} '{Title}' created on {PageType}/{PageId} with {FileCount} attached files",
                    section.Id, section.Title, section.PageType, section.PageId, savedPaths.Count);

                TempData["Success"] = "Section added successfully.";
                return RedirectToAction(nameof(Index), new { pageType = model.PageType, pageId = model.PageId });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error creating section '{Title}' on {PageType}/{PageId}",
                    model.Title, model.PageType, model.PageId);
                ModelState.AddModelError(string.Empty, "Unable to save the section. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error creating section '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Section";

            var section = await _context.DynamicSections
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section == null) return NotFound();

            var vm = new DynamicSectionVM
            {
                Id = section.Id,
                Title = section.Title,
                SectionType = section.SectionType,
                HtmlContent = section.HtmlContent,
                FilePath = section.FilePath,
                FileName = section.FileName,
                PageType = section.PageType,
                PageId = section.PageId,
                DisplayOrder = section.DisplayOrder,
                IsVisible = section.IsVisible,
                SectionTypes = GetSectionTypeList()
            };

            ViewBag.ExistingFiles = section.Files
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.DisplayOrder).ToList();

            return View(vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DynamicSectionVM model, IFormFile? UploadFile)
        {
            ViewData["Title"] = "Edit Section";
            model.SectionTypes = GetSectionTypeList();

            if (!ModelState.IsValid)
                return View(model);

            var section = await _context.DynamicSections.FindAsync(id);
            if (section == null) return NotFound();

            bool isPdfType = model.SectionType == SectionType.PDFViewer ||
                              model.SectionType == SectionType.PDFDownload;
            bool replacingFile = UploadFile != null && UploadFile.Length > 0 && isPdfType;

            string? newFilePath = null;
            if (replacingFile)
            {
                var result = await _fileStorage.SaveAsync(UploadFile, SectionsFolder, FileCategory.Document);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(UploadFile), result.ErrorMessage!);
                    return View(model);
                }
                newFilePath = result.RelativePath!;
            }

            string? previousFilePath = section.FilePath;

            section.Title = model.Title;
            section.HtmlContent = model.HtmlContent;
            section.IsVisible = model.IsVisible;

            if (replacingFile)
            {
                section.FilePath = newFilePath;
                section.FileName = UploadFile!.FileName;
            }

            try
            {
                await _context.SaveChangesAsync();

                // Only remove the old file once the new state is safely persisted.
                if (replacingFile)
                    _fileStorage.Delete(previousFilePath);

                _logger.LogInformation("Section {SectionId} updated", section.Id);

                TempData["Success"] = "Section updated.";
                return RedirectToAction(nameof(Index), new { pageType = section.PageType, pageId = section.PageId });
            }
            catch (DbUpdateException ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Database error updating section {SectionId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the section. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Unexpected error updating section {SectionId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var section = await _context.DynamicSections.FindAsync(id);
            if (section == null) return NotFound();

            section.IsVisible = !section.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Section {SectionId} visibility set to {IsVisible}", id, section.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for section {SectionId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { pageType = section.PageType, pageId = section.PageId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.DynamicSections
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section == null) return NotFound();

            var pageType = section.PageType;
            var pageId = section.PageId;
            var filePathsToDelete = new List<string?> { section.FilePath };
            filePathsToDelete.AddRange(section.Files.Select(f => f.FilePath));

            section.IsDeleted = true;
            foreach (var f in section.Files)
                f.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical files are removed only after the soft-delete is committed.
                foreach (var path in filePathsToDelete)
                    _fileStorage.Delete(path);

                _logger.LogInformation("Section {SectionId} deleted with {FileCount} attached files",
                    id, filePathsToDelete.Count(p => !string.IsNullOrEmpty(p)));

                TempData["Success"] = "Section deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting section {SectionId}", id);
                TempData["Error"] = "Unable to delete the section. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { pageType, pageId });
        }

        // ── REORDER (AJAX) ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveOrder([FromBody] List<ReorderItem> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest();

            try
            {
                var ids = items.Select(i => i.Id).ToList();
                var sections = await _context.DynamicSections
                    .Where(s => ids.Contains(s.Id))
                    .ToListAsync();

                var orderById = items.ToDictionary(i => i.Id, i => i.Order);
                foreach (var section in sections)
                    section.DisplayOrder = orderById[section.Id];

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving section order");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // ── DELETE FILE FROM GALLERY ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            var file = await _context.DynamicSectionFiles.FindAsync(fileId);
            if (file == null) return NotFound();

            int sectionId = file.DynamicSectionId;
            string? filePath = file.FilePath;
            file.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                _fileStorage.Delete(filePath);

                _logger.LogInformation("File {FileId} removed from section {SectionId}", fileId, sectionId);
                TempData["Success"] = "File removed.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error removing file {FileId} from section {SectionId}", fileId, sectionId);
                TempData["Error"] = "Unable to remove the file. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = sectionId });
        }

        // ── ADD FILES TO EXISTING SECTION ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFiles(int sectionId)
        {
            var section = await _context.DynamicSections
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null) return NotFound();

            if (Request.Form.Files.Count == 0)
            {
                TempData["Error"] = "No files were selected.";
                return RedirectToAction(nameof(Edit), new { id = sectionId });
            }

            var category = section.SectionType == SectionType.ImageGallery
                ? FileCategory.Image : FileCategory.Document;
            string fileType = section.SectionType == SectionType.ImageGallery ? "Image" : "PDF";

            var savedPaths = new List<string>();
            var newFiles = new List<(string Path, string Title)>();

            foreach (var file in Request.Form.Files)
            {
                if (file.Length == 0) continue;

                var result = await _fileStorage.SaveAsync(file, SectionsFolder, category);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    TempData["Error"] = $"'{file.FileName}': {result.ErrorMessage}";
                    return RedirectToAction(nameof(Edit), new { id = sectionId });
                }

                savedPaths.Add(result.RelativePath!);
                newFiles.Add((result.RelativePath!, Path.GetFileNameWithoutExtension(file.FileName)));
            }

            int order = section.Files.Any() ? section.Files.Max(f => f.DisplayOrder) + 1 : 0;

            try
            {
                foreach (var (path, title) in newFiles)
                {
                    _context.DynamicSectionFiles.Add(new DynamicSectionFile
                    {
                        DynamicSectionId = sectionId,
                        FilePath = path,
                        Title = title,
                        FileType = fileType,
                        DisplayOrder = order++
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} file(s) added to section {SectionId}", newFiles.Count, sectionId);
                TempData["Success"] = "Files added.";
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error adding files to section {SectionId}", sectionId);
                TempData["Error"] = "Unable to save the uploaded files. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = sectionId });
        }

        // ── HELPERS ───────────────────────────────────────
        private List<SelectListItem> GetSectionTypeList() => new()
        {
            new SelectListItem("Rich Text (HTML Editor)", "1"),
            new SelectListItem("PDF Viewer (Inline)",     "2"),
            new SelectListItem("PDF Download (Button)",   "3"),
            new SelectListItem("Image Gallery",           "4"),
            new SelectListItem("File List",               "5"),
            new SelectListItem("Table",                   "6"),
        };

        private async Task<string> GetPageNameAsync(PageType pageType, int pageId)
        {
            if (pageType == PageType.Department)
            {
                var dept = await _context.Departments.FindAsync(pageId);
                return dept?.Name ?? "Unknown";
            }
            if (pageType == PageType.Committee)
            {
                var comm = await _context.CampusCommittees.FindAsync(pageId);
                return comm?.Title ?? "Unknown";
            }
            return $"Page {pageId}";
        }
    }

    public class ReorderItem
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}