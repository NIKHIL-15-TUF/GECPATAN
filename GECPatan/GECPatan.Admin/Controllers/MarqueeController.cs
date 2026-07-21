using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        private readonly IWebHostEnvironment _env;

        public MarqueeController(ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
        public async Task<IActionResult> Create(
            MarqueeFormVM model, IFormFile? UploadFile)
        {
            ViewData["Title"] = "Add Marquee Item";
            ValidateForm(model);
            if (!ModelState.IsValid)
            {
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            int maxPos = await _context.Marquees
                .Select(m => (int?)m.DisplayOrder).MaxAsync() ?? -1;

            var item = BuildEntity(model);
            item.DisplayOrder = maxPos + 1;

            if (UploadFile != null && UploadFile.Length > 0)
                item.FilePath = await SaveFileAsync(UploadFile, "marquee");

            _context.Marquees.Add(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Marquee item '{item.Title}' added.";
            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Edit(
            int id, MarqueeFormVM model, IFormFile? UploadFile)
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

            UpdateEntity(item, model);

            if (UploadFile != null && UploadFile.Length > 0)
            {
                DeleteFile(item.FilePath);
                item.FilePath = await SaveFileAsync(UploadFile, "marquee");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.Title}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();
            item.IsVisible = !item.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.Title}' "
                + (item.IsVisible ? "shown" : "hidden") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Marquees.FindAsync(id);
            if (item == null) return NotFound();
            DeleteFile(item.FilePath);
            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
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

            await _context.SaveChangesAsync();
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
            if (string.IsNullOrEmpty(vm.DynamicType) ||
                vm.LinkType != "dynamic") return;

            vm.DynamicIdOptions = vm.DynamicType switch
            {
                "Department" => await _context.Departments
                    .Where(d => d.IsActive).OrderBy(d => d.Name)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DeptId.ToString(),
                        Text = d.Name
                    }).ToListAsync(),

                "Committee" => await _context.CampusCommittees
                    .OrderBy(c => c.Title)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Title
                    }).ToListAsync(),

                "Facility" => await _context.Facilities
                    .Where(f => f.IsActive).OrderBy(f => f.Title)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Title
                    }).ToListAsync(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title
                    }).ToListAsync(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Title
                    }).ToListAsync(),

                _ => new List<SelectListItem>()
            };
        }

        private void ValidateForm(MarqueeFormVM m)
        {
            if (m.LinkType == "internal" &&
                string.IsNullOrWhiteSpace(m.ControllerName))
                ModelState.AddModelError("ControllerName",
                    "Controller is required for Internal links.");

            if (m.LinkType == "dynamic")
            {
                if (string.IsNullOrWhiteSpace(m.DynamicType))
                    ModelState.AddModelError("DynamicType",
                        "Please select a dynamic type.");
                if (!m.DynamicId.HasValue)
                    ModelState.AddModelError("DynamicId",
                        "Please select an item.");
            }

            if (m.LinkType == "external" &&
                string.IsNullOrWhiteSpace(m.ExternalLink))
                ModelState.AddModelError("ExternalLink",
                    "URL is required for External links.");

            if (m.ValidFrom.HasValue && m.ValidTo.HasValue &&
                m.ValidTo < m.ValidFrom)
                ModelState.AddModelError("ValidTo",
                    "Valid To must be after Valid From.");
        }

        private static Marquee BuildEntity(MarqueeFormVM m) => new()
        {
            Title = m.Title,
            LinkType = m.LinkType,
            ControllerName = m.LinkType is "internal" or "dynamic"
                ? m.ControllerName : null,
            ActionName = m.LinkType is "internal" or "dynamic"
                ? m.ActionName : null,
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
            e.ControllerName = m.LinkType is "internal" or "dynamic"
                ? m.ControllerName : null;
            e.ActionName = m.LinkType is "internal" or "dynamic"
                ? m.ActionName : null;
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
