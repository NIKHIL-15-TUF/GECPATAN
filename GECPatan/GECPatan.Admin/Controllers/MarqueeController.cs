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
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class MarqueeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<MarqueeController> _logger;
        private const string MarqueeFolder = "marquee";

        public MarqueeController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<MarqueeController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Marquee / Notice Ticker";

            var now = DateTime.Now;
            var items = await _context.Marquees
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var list = items.Select(m => new MarqueeListVM
            {
                Id = m.Id,
                Title = m.Title,
                LinkType = m.LinkType,
                LinkDescription = BuildLinkDesc(m),
                ValidFrom = m.ValidFrom,
                ValidTo = m.ValidTo,
                IsVisible = m.IsVisible,
                IsActive = m.IsVisible
                    && (!m.ValidFrom.HasValue || m.ValidFrom <= now)
                    && (!m.ValidTo.HasValue || m.ValidTo >= now),
                DisplayOrder = m.DisplayOrder,
                HorizontalMarquee = m.HorizontalMarquee
            }).ToList();

            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Marquee Item";
            return View("Form", new MarqueeFormVM
            {
                ValidFrom = DateTime.Today,
                ValidTo = DateTime.Today.AddMonths(1)
            });
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarqueeFormVM model, IFormFile? UploadFile)
        {
            ViewData["Title"] = "Add Marquee Item";
            ValidateForm(model);
            if (!ModelState.IsValid)
            {
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            string? filePath = null;
            bool hasFile = UploadFile != null && UploadFile.Length > 0;
            if (hasFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(UploadFile!, MarqueeFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(UploadFile), uploadResult.ErrorMessage!);
                    await LoadDynamicOptions(model);
                    return View("Form", model);
                }
                filePath = uploadResult.RelativePath;
            }

            int maxPos = await _context.Marquees
                .Select(m => (int?)m.DisplayOrder).MaxAsync() ?? -1;

            var item = BuildEntity(model);
            item.DisplayOrder = maxPos + 1;
            item.FilePath = filePath;

            try
            {
                _context.Marquees.Add(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marquee item {MarqueeId} '{Title}' created", item.Id, item.Title);

                TempData["Success"] = $"Marquee item '{item.Title}' added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Database error creating marquee item '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the marquee item. Please try again.");
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(filePath);
                _logger.LogError(ex, "Unexpected error creating marquee item '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Marquee Item";
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();

            var vm = EntityToForm(item);
            await LoadDynamicOptions(vm);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarqueeFormVM model, IFormFile? UploadFile)
        {
            ViewData["Title"] = "Edit Marquee Item";
            ValidateForm(model);
            if (!ModelState.IsValid)
            {
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();

            string? newFilePath = null;
            bool replacingFile = UploadFile != null && UploadFile.Length > 0;
            if (replacingFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(UploadFile!, MarqueeFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(UploadFile), uploadResult.ErrorMessage!);
                    await LoadDynamicOptions(model);
                    return View("Form", model);
                }
                newFilePath = uploadResult.RelativePath;
            }

            string? previousFilePath = item.FilePath;

            UpdateEntity(item, model);

            if (replacingFile)
                item.FilePath = newFilePath;

            try
            {
                await _context.SaveChangesAsync();

                // Old file is only removed once the new state is safely persisted.
                if (replacingFile)
                    _fileStorage.Delete(previousFilePath);

                _logger.LogInformation("Marquee item {MarqueeId} '{Title}' updated", item.Id, item.Title);

                TempData["Success"] = $"'{item.Title}' updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Database error updating marquee item {MarqueeId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the marquee item. Please try again.");
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Unexpected error updating marquee item {MarqueeId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVisible = !item.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Marquee item {MarqueeId} visibility set to {IsVisible}", id, item.IsVisible);
                TempData["Success"] = $"'{item.Title}' " + (item.IsVisible ? "shown" : "hidden") + ".";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for marquee item {MarqueeId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();

            string? filePath = item.FilePath;
            item.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _fileStorage.Delete(filePath);

                _logger.LogInformation("Marquee item {MarqueeId} '{Title}' deleted", id, item.Title);
                TempData["Success"] = $"'{item.Title}' deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting marquee item {MarqueeId}", id);
                TempData["Error"] = "Unable to delete the item. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string dir)
        {
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();

            var all = await _context.Marquees
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            int idx = all.FindIndex(m => m.Id == id);

            if (dir == "up" && idx > 0)
            {
                all[idx].DisplayOrder--;
                all[idx - 1].DisplayOrder++;
            }
            else if (dir == "down" && idx < all.Count - 1)
            {
                all[idx].DisplayOrder++;
                all[idx + 1].DisplayOrder--;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering marquee item {MarqueeId}", id);
                TempData["Error"] = "Unable to reorder items. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── GET DYNAMIC OPTIONS (AJAX) ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDynamicOptions(string type)
        {
            var items = type switch
            {
                "Department" => await _context.Departments
                    .Where(d => d.IsActive).OrderBy(d => d.Name)
                    .Select(d => new { id = d.DeptId, text = d.Name })
                    .ToListAsync<object>(),

                "Committee" => await _context.CampusCommittees
                    .OrderBy(c => c.Title)
                    .Select(c => new { id = c.Id, text = c.Title })
                    .ToListAsync<object>(),

                "Facility" => await _context.Facilities
                    .Where(f => f.IsActive).OrderBy(f => f.Title)
                    .Select(f => new { id = f.Id, text = f.Title })
                    .ToListAsync<object>(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
                    .Select(p => new { id = p.Id, text = p.Title })
                    .ToListAsync<object>(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new { id = d.Id, text = d.Title })
                    .ToListAsync<object>(),

                _ => new List<object>()
            };
            return Json(items);
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private static string BuildLinkDesc(Marquee m) => m.LinkType switch
        {
            "internal" => $"{m.ControllerName}/{m.ActionName}",
            "dynamic" => $"{m.DynamicType} #{m.DynamicId}",
            "external" => m.ExternalLink ?? "",
            "file" => m.FilePath != null ? "PDF/File" : "—",
            _ => "No link"
        };

        private async Task LoadDynamicOptions(MarqueeFormVM vm)
        {
            if (string.IsNullOrEmpty(vm.DynamicType) || vm.LinkType != "dynamic") return;

            vm.DynamicIdOptions = vm.DynamicType switch
            {
                "Department" => await _context.Departments
                    .Where(d => d.IsActive).OrderBy(d => d.Name)
                    .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name })
                    .ToListAsync(),

                "Committee" => await _context.CampusCommittees
                    .OrderBy(c => c.Title)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title })
                    .ToListAsync(),

                "Facility" => await _context.Facilities
                    .Where(f => f.IsActive).OrderBy(f => f.Title)
                    .Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Title })
                    .ToListAsync(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title })
                    .ToListAsync(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Title })
                    .ToListAsync(),

                _ => new List<SelectListItem>()
            };
        }

        private void ValidateForm(MarqueeFormVM m)
        {
            if (m.LinkType == "internal" && string.IsNullOrWhiteSpace(m.ControllerName))
                ModelState.AddModelError(nameof(m.ControllerName), "Controller is required for Internal links.");

            if (m.LinkType == "dynamic")
            {
                if (string.IsNullOrWhiteSpace(m.DynamicType))
                    ModelState.AddModelError(nameof(m.DynamicType), "Please select a dynamic type.");
                if (!m.DynamicId.HasValue)
                    ModelState.AddModelError(nameof(m.DynamicId), "Please select an item.");
            }

            if (m.LinkType == "external" && string.IsNullOrWhiteSpace(m.ExternalLink))
                ModelState.AddModelError(nameof(m.ExternalLink), "URL is required for External links.");

            if (m.ValidFrom.HasValue && m.ValidTo.HasValue && m.ValidTo < m.ValidFrom)
                ModelState.AddModelError(nameof(m.ValidTo), "Valid To must be after Valid From.");
        }

        private static Marquee BuildEntity(MarqueeFormVM m) => new()
        {
            Title = m.Title,
            LinkType = m.LinkType,
            ControllerName = m.LinkType is "internal" or "dynamic" ? m.ControllerName : null,
            ActionName = m.LinkType is "internal" or "dynamic" ? m.ActionName : null,
            DynamicType = m.LinkType == "dynamic" ? m.DynamicType : null,
            DynamicId = m.LinkType == "dynamic" ? m.DynamicId : null,
            ExternalLink = m.LinkType == "external" ? m.ExternalLink : null,
            ValidFrom = m.ValidFrom,
            ValidTo = m.ValidTo,
            DisplayOrder = m.DisplayOrder,
            IsVisible = m.IsVisible,
            HorizontalMarquee = m.HorizontalMarquee
        };

        private static void UpdateEntity(Marquee e, MarqueeFormVM m)
        {
            e.Title = m.Title;
            e.LinkType = m.LinkType;
            e.ControllerName = m.LinkType is "internal" or "dynamic" ? m.ControllerName : null;
            e.ActionName = m.LinkType is "internal" or "dynamic" ? m.ActionName : null;
            e.DynamicType = m.LinkType == "dynamic" ? m.DynamicType : null;
            e.DynamicId = m.LinkType == "dynamic" ? m.DynamicId : null;
            e.ExternalLink = m.LinkType == "external" ? m.ExternalLink : null;
            e.ValidFrom = m.ValidFrom;
            e.ValidTo = m.ValidTo;
            e.DisplayOrder = m.DisplayOrder;
            e.IsVisible = m.IsVisible;
            e.HorizontalMarquee = m.HorizontalMarquee;
        }

        private static MarqueeFormVM EntityToForm(Marquee m)
        {
            string linkType = "none";
            if (!string.IsNullOrEmpty(m.FilePath)) linkType = "file";
            else if (!string.IsNullOrEmpty(m.ExternalLink)) linkType = "external";
            else if (m.DynamicId.HasValue) linkType = "dynamic";
            else if (!string.IsNullOrEmpty(m.ControllerName)) linkType = "internal";

            return new MarqueeFormVM
            {
                Id = m.Id,
                Title = m.Title,
                LinkType = linkType,
                ControllerName = m.ControllerName,
                ActionName = m.ActionName,
                DynamicType = m.DynamicType,
                DynamicId = m.DynamicId,
                ExternalLink = m.ExternalLink,
                ExistingFilePath = m.FilePath,
                ValidFrom = m.ValidFrom,
                ValidTo = m.ValidTo,
                DisplayOrder = m.DisplayOrder,
                IsVisible = m.IsVisible,
                HorizontalMarquee = m.HorizontalMarquee
            };
        }
    }
}