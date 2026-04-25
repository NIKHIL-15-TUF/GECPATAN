using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuItemController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(string menuType = "Main")
        {
            ViewData["Title"] = menuType == "Footer"
                ? "Footer Menu" : "Navigation Menu";
            ViewBag.MenuType = menuType;

            var all = await _context.MenuItems
                .Where(m => m.MenuType == menuType && !m.IsDeleted)
                .OrderBy(m => m.Position)
                .ToListAsync();

            // Resolve dynamic labels
            var deptMap = await _context.Departments
                .ToDictionaryAsync(d => d.DeptId, d => d.Name);
            var commMap = await _context.CampusCommittees
                .ToDictionaryAsync(c => c.Id, c => c.Title);
            var facMap = await _context.Facilities
                .ToDictionaryAsync(f => f.Id, f => f.Title);
            var clubMap = await _context.StudentClubs
                .ToDictionaryAsync(c => c.Id, c => c.Title);
            var docMap = await _context.DocumentCategories
                .ToDictionaryAsync(d => d.Id, d => d.Title);
            var pageMap = await _context.ContentPages
                .ToDictionaryAsync(p => p.Id, p => p.Title);

            string ResolveLabel(MenuItem m)
            {
                if (!m.DynamicId.HasValue) return "";
                int id = m.DynamicId.Value;
                return m.DynamicType switch
                {
                    "Department" => deptMap.GetValueOrDefault(id) ?? $"#{id}",
                    "Committee" => commMap.GetValueOrDefault(id) ?? $"#{id}",
                    "Facility" => facMap.GetValueOrDefault(id) ?? $"#{id}",
                    "Club" => clubMap.GetValueOrDefault(id) ?? $"#{id}",
                    "Document" => docMap.GetValueOrDefault(id) ?? $"#{id}",
                    "ContentPage" => pageMap.GetValueOrDefault(id) ?? $"#{id}",
                    _ => $"#{id}"
                };
            }

            var tree = BuildTree(all, null, 0, ResolveLabel);
            return View(tree);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create(
            int? parentId, string menuType = "Main")
        {
            ViewData["Title"] = "Add Menu Item";
            var vm = new MenuItemFormVM
            {
                ParentId = parentId,
                MenuType = menuType
            };
            await LoadParentOptions(vm, menuType);
            return View("Form", vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemFormVM model)
        {
            ViewData["Title"] = "Add Menu Item";
            await ValidateForm(model);
            if (!ModelState.IsValid)
            {
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            int maxPos = await _context.MenuItems
                .Where(m => m.ParentId == model.ParentId
                         && m.MenuType == model.MenuType
                         && !m.IsDeleted)
                .Select(m => (int?)m.Position).MaxAsync() ?? -1;

            var item = BuildEntity(model);
            item.Position = maxPos + 1;
            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{item.MenuText}' added.";
            return RedirectToAction(nameof(Index),
                new { menuType = model.MenuType });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Menu Item";
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var vm = EntityToForm(item);
            await LoadParentOptions(vm, item.MenuType);
            await LoadDynamicOptions(vm);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemFormVM model)
        {
            ViewData["Title"] = "Edit Menu Item";
            await ValidateForm(model);

            // Circular check
            if (model.ParentId == id)
                ModelState.AddModelError("ParentId",
                    "An item cannot be its own parent.");

            if (!ModelState.IsValid)
            {
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            UpdateEntity(item, model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{item.MenuText}' updated.";
            return RedirectToAction(nameof(Index),
                new { menuType = item.MenuType });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();
            item.IsVisible = !item.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.MenuText}' "
                + (item.IsVisible ? "shown" : "hidden") + ".";
            return RedirectToAction(nameof(Index),
                new { menuType = item.MenuType });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();
            string mType = item.MenuType;

            bool hasChildren = await _context.MenuItems
                .AnyAsync(m => m.ParentId == id && !m.IsDeleted);

            if (hasChildren)
            {
                TempData["Error"] =
                    $"Cannot delete '{item.MenuText}' — "
                    + "it has child items. Remove children first.";
                return RedirectToAction(nameof(Index),
                    new { menuType = mType });
            }

            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.MenuText}' deleted.";
            return RedirectToAction(nameof(Index),
                new { menuType = mType });
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string dir)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var siblings = await _context.MenuItems
                .Where(m => m.ParentId == item.ParentId
                         && m.MenuType == item.MenuType
                         && !m.IsDeleted)
                .OrderBy(m => m.Position)
                .ToListAsync();

            int idx = siblings.FindIndex(m => m.Id == id);

            if (dir == "up" && idx > 0)
            {
                siblings[idx].Position--;
                siblings[idx - 1].Position++;
            }
            else if (dir == "down" && idx < siblings.Count - 1)
            {
                siblings[idx].Position++;
                siblings[idx + 1].Position--;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { menuType = item.MenuType });
        }

        // ── GET DYNAMIC OPTIONS (AJAX) ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDynamicOptions(string type)
        {
            var items = type switch
            {
                "Department" => await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .Select(d => new { id = d.DeptId, text = d.Name })
                    .ToListAsync<object>(),

                "Committee" => await _context.CampusCommittees
                    .OrderBy(c => c.Title)
                    .Select(c => new { id = c.Id, text = c.Title })
                    .ToListAsync<object>(),

                "Facility" => await _context.Facilities
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Title)
                    .Select(f => new { id = f.Id, text = f.Title })
                    .ToListAsync<object>(),

                "Club" => await _context.StudentClubs
                    .Where(c => c.IsVisible)
                    .OrderBy(c => c.Title)
                    .Select(c => new { id = c.Id, text = c.Title })
                    .ToListAsync<object>(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible)
                    .OrderBy(d => d.Title)
                    .Select(d => new { id = d.Id, text = d.Title })
                    .ToListAsync<object>(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible)
                    .OrderBy(p => p.Title)
                    .Select(p => new { id = p.Id, text = p.Title })
                    .ToListAsync<object>(),

                _ => new List<object>()
            };
            return Json(items);
        }

        // ══════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════
        private static List<MenuItemTreeVM> BuildTree(
            List<MenuItem> all, int? parentId, int level,
            Func<MenuItem, string> resolveLabel)
        {
            var siblings = all
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.Position)
                .ToList();

            return siblings.Select((m, idx) => new MenuItemTreeVM
            {
                Id = m.Id,
                MenuText = m.MenuText,
                ParentId = m.ParentId,
                LinkType = m.LinkType,
                ControllerName = m.ControllerName,
                ActionName = m.ActionName,
                DynamicId = m.DynamicId,
                DynamicType = m.DynamicType,
                DynamicLabel = resolveLabel(m),
                ExternalLink = m.ExternalLink,
                CssClass = m.CssClass,
                MenuType = m.MenuType,
                Position = m.Position,
                IsVisible = m.IsVisible,
                OpenInNewTab = m.OpenInNewTab,
                Level = level,
                IsFirst = idx == 0,
                IsLast = idx == siblings.Count - 1,
                Children = BuildTree(all, m.Id, level + 1, resolveLabel)
            }).ToList();
        }

        private async Task LoadParentOptions(
            MenuItemFormVM vm, string menuType)
        {
            var all = await _context.MenuItems
                .Where(m => m.MenuType == menuType
                         && !m.IsDeleted
                         && m.Id != vm.Id)
                .OrderBy(m => m.Position)
                .ToListAsync();

            var opts = new List<SelectListItem>
            {
                new() { Value = "", Text = "— Top Level (no parent) —" }
            };

            // Level 1 items
            foreach (var top in all.Where(m => m.ParentId == null))
            {
                opts.Add(new SelectListItem
                {
                    Value = top.Id.ToString(),
                    Text = top.MenuText
                });
                // Level 2 items (can also be parents for 3rd level)
                foreach (var sub in all.Where(m => m.ParentId == top.Id))
                {
                    opts.Add(new SelectListItem
                    {
                        Value = sub.Id.ToString(),
                        Text = $"  └─ {sub.MenuText}"
                    });
                }
            }

            vm.ParentOptions = opts;
        }

        private async Task LoadDynamicOptions(MenuItemFormVM vm)
        {
            if (string.IsNullOrEmpty(vm.DynamicType) ||
                vm.LinkType != "dynamic")
            {
                vm.DynamicIdOptions = new();
                return;
            }
            vm.DynamicIdOptions = await GetDynamicList(vm.DynamicType);
        }

        private async Task<List<SelectListItem>> GetDynamicList(string type)
        {
            return type switch
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

                "Club" => await _context.StudentClubs
                    .Where(c => c.IsVisible).OrderBy(c => c.Title)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Title
                    }).ToListAsync(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Title
                    }).ToListAsync(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title
                    }).ToListAsync(),

                _ => new List<SelectListItem>()
            };
        }

        private Task ValidateForm(MenuItemFormVM m)
        {
            if (m.LinkType == "internal" &&
                string.IsNullOrWhiteSpace(m.ControllerName))
                ModelState.AddModelError("ControllerName",
                    "Controller name is required for Internal links.");

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
                    "URL or PDF path is required for External links.");

            return Task.CompletedTask;
        }

        private static string DefaultController(string? type) => type switch
        {
            "Department" => "Department",
            "Committee" => "CampusCommittee",
            "Facility" => "Facility",
            "Club" => "StudentCorner",
            "Document" => "Documents",
            "ContentPage" => "Page",
            _ => ""
        };

        private static string DefaultAction(string? type) => type switch
        {
            "Department" => "DepartmentDetails",
            "Committee" => "CommitteePage",
            "Facility" => "FacilityPage",
            "Club" => "ClubPage",
            "Document" => "Index",
            "ContentPage" => "View",
            _ => ""
        };

        private static MenuItem BuildEntity(MenuItemFormVM m) =>
            new()
            {
                MenuText = m.MenuText,
                ParentId = m.ParentId == 0 ? null : m.ParentId,
                LinkType = m.LinkType,
                ControllerName = m.LinkType is "internal" or "dynamic"
                    ? m.ControllerName : null,
                ActionName = m.LinkType is "internal" or "dynamic"
                    ? m.ActionName : null,
                DynamicType = m.LinkType == "dynamic" ? m.DynamicType : null,
                DynamicId = m.LinkType == "dynamic" ? m.DynamicId : null,
                ExternalLink = m.LinkType == "external" ? m.ExternalLink : null,
                CssClass = m.CssClass,
                MenuType = m.MenuType,
                Position = m.Position,
                IsVisible = m.IsVisible,
                OpenInNewTab = m.OpenInNewTab
            };

        private static void UpdateEntity(MenuItem e, MenuItemFormVM m)
        {
            e.MenuText = m.MenuText;
            e.ParentId = m.ParentId == 0 ? null : m.ParentId;
            e.LinkType = m.LinkType;
            e.ControllerName = m.LinkType is "internal" or "dynamic"
                ? m.ControllerName : null;
            e.ActionName = m.LinkType is "internal" or "dynamic"
                ? m.ActionName : null;
            e.DynamicType = m.LinkType == "dynamic" ? m.DynamicType : null;
            e.DynamicId = m.LinkType == "dynamic" ? m.DynamicId : null;
            e.ExternalLink = m.LinkType == "external" ? m.ExternalLink : null;
            e.CssClass = m.CssClass;
            e.MenuType = m.MenuType;
            e.Position = m.Position;
            e.IsVisible = m.IsVisible;
            e.OpenInNewTab = m.OpenInNewTab;
        }

        private static MenuItemFormVM EntityToForm(MenuItem m) => new()
        {
            Id = m.Id,
            MenuText = m.MenuText,
            ParentId = m.ParentId,
            LinkType = m.LinkType,
            ControllerName = m.ControllerName,
            ActionName = m.ActionName,
            DynamicType = m.DynamicType,
            DynamicId = m.DynamicId,
            ExternalLink = m.ExternalLink,
            CssClass = m.CssClass,
            MenuType = m.MenuType,
            Position = m.Position,
            IsVisible = m.IsVisible,
            OpenInNewTab = m.OpenInNewTab
        };
    }
}