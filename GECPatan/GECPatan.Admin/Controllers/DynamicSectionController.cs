using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        private readonly IWebHostEnvironment _env;

        public DynamicSectionController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                IsVisible = true
            };

            // Handle file upload for PDF types
            if (UploadFile != null && UploadFile.Length > 0 &&
                (model.SectionType == SectionType.PDFViewer ||
                 model.SectionType == SectionType.PDFDownload))
            {
                section.FilePath = await SaveFileAsync(UploadFile, "sections");
                section.FileName = UploadFile.FileName;
            }

            _context.DynamicSections.Add(section);
            await _context.SaveChangesAsync();

            // Handle gallery / file list uploads
            if ((model.SectionType == SectionType.ImageGallery ||
                 model.SectionType == SectionType.FileList) &&
                Request.Form.Files.Count > 0)
            {
                int order = 0;
                foreach (var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var path = await SaveFileAsync(file, "sections");
                        _context.DynamicSectionFiles.Add(new DynamicSectionFile
                        {
                            DynamicSectionId = section.Id,
                            FilePath = path,
                            Title = Path.GetFileNameWithoutExtension(file.FileName),
                            FileType = model.SectionType == SectionType.ImageGallery
                                               ? "Image" : "PDF",
                            DisplayOrder = order++
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Section added successfully.";
            return RedirectToAction(nameof(Index),
                new { pageType = model.PageType, pageId = model.PageId });
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

            section.Title = model.Title;
            section.HtmlContent = model.HtmlContent;
            section.IsVisible = model.IsVisible;

            if (UploadFile != null && UploadFile.Length > 0 &&
                (model.SectionType == SectionType.PDFViewer ||
                 model.SectionType == SectionType.PDFDownload))
            {
                DeleteFile(section.FilePath);
                section.FilePath = await SaveFileAsync(UploadFile, "sections");
                section.FileName = UploadFile.FileName;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Section updated.";
            return RedirectToAction(nameof(Index),
                new { pageType = section.PageType, pageId = section.PageId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var section = await _context.DynamicSections.FindAsync(id);
            if (section == null) return NotFound();

            section.IsVisible = !section.IsVisible;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index),
                new { pageType = section.PageType, pageId = section.PageId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.DynamicSections
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section == null) return NotFound();

            var pageType = section.PageType;
            var pageId = section.PageId;

            // Delete associated files
            DeleteFile(section.FilePath);
            foreach (var f in section.Files)
                DeleteFile(f.FilePath);

            section.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Section deleted.";
            return RedirectToAction(nameof(Index),
                new { pageType, pageId });
        }

        // ── REORDER (AJAX) ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveOrder(
            [FromBody] List<ReorderItem> items)
        {
            foreach (var item in items)
            {
                var section = await _context.DynamicSections.FindAsync(item.Id);
                if (section != null)
                    section.DisplayOrder = item.Order;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ── DELETE FILE FROM GALLERY ──────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            var file = await _context.DynamicSectionFiles.FindAsync(fileId);
            if (file == null) return NotFound();

            int sectionId = file.DynamicSectionId;
            var section = await _context.DynamicSections.FindAsync(sectionId);

            DeleteFile(file.FilePath);
            file.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "File removed.";
            return RedirectToAction(nameof(Edit), new { id = sectionId });
        }

        // ── ADD FILES TO EXISTING SECTION ─────────────────
        [HttpPost]
        public async Task<IActionResult> AddFiles(int sectionId)
        {
            var section = await _context.DynamicSections
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null) return NotFound();

            int order = section.Files.Any()
                ? section.Files.Max(f => f.DisplayOrder) + 1 : 0;

            foreach (var file in Request.Form.Files)
            {
                if (file.Length > 0)
                {
                    var path = await SaveFileAsync(file, "sections");
                    _context.DynamicSectionFiles.Add(new DynamicSectionFile
                    {
                        DynamicSectionId = sectionId,
                        FilePath = path,
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        FileType = section.SectionType == SectionType.ImageGallery
                                           ? "Image" : "PDF",
                        DisplayOrder = order++
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Files added.";
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
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }

    public class ReorderItem
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}