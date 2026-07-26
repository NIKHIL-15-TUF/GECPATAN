using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        private readonly ILogger<MenuItemController> _logger;

        public MenuItemController(ApplicationDbContext context, ILogger<MenuItemController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(string menuType = "Main")
        {
            ViewData["Title"] = menuType == "Footer" ? "Footer Menu" : "Navigation Menu";
            ViewBag.MenuType = menuType;

            var all = await _context.MenuItems
                .Where(m => m.MenuType == menuType && !m.IsDeleted)
                .OrderBy(m => m.Position)
                .ToListAsync();

            var deptMap = await _context.Departments.ToDictionaryAsync(d => d.DeptId, d => d.Name);
            var commMap = await _context.CampusCommittees.ToDictionaryAsync(c => c.Id, c => c.Title);
            var facMap = await _context.Facilities.ToDictionaryAsync(f => f.Id, f => f.Title);
            var clubMap = await _context.StudentClubs.ToDictionaryAsync(c => c.Id, c => c.Title);
            var docMap = await _context.DocumentCategories.ToDictionaryAsync(d => d.Id, d => d.Title);
            var pageMap = await _context.ContentPages.ToDictionaryAsync(p => p.Id, p => p.Title);

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
        public async Task<IActionResult> Create(int? parentId, string menuType = "Main")
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
            ValidateForm(model);
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

            try
            {
                _context.MenuItems.Add(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Menu item {MenuItemId} '{MenuText}' created ({MenuType})",
                    item.Id, item.MenuText, item.MenuType);

                TempData["Success"] = $"'{item.MenuText}' added.";
                return RedirectToAction(nameof(Index), new { menuType = model.MenuType });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating menu item '{MenuText}'", model.MenuText);
                ModelState.AddModelError(string.Empty, "Unable to save the menu item. Please try again.");
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating menu item '{MenuText}'", model.MenuText);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
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
            ValidateForm(model);

            if (model.ParentId == id)
            {
                ModelState.AddModelError(nameof(model.ParentId), "An item cannot be its own parent.");
            }
            else if (model.ParentId.HasValue &&
                     await WouldCreateCycleAsync(id, model.ParentId.Value))
            {
                // Original code only caught the direct self-parent case (ParentId == id).
                // Assigning a menu item as a child of one of its own descendants was still
                // possible and would corrupt the tree (infinite loop in BuildTree's recursion,
                // and the item would silently vanish from Index since it filters by ParentId
                // chains reachable from the root). This walks the proposed parent's ancestry
                // chain to catch that case too.
                ModelState.AddModelError(nameof(model.ParentId),
                    "Cannot move this item under one of its own descendants.");
            }

            if (!ModelState.IsValid)
            {
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }

            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            UpdateEntity(item, model);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Menu item {MenuItemId} '{MenuText}' updated", item.Id, item.MenuText);

                TempData["Success"] = $"'{item.MenuText}' updated.";
                return RedirectToAction(nameof(Index), new { menuType = item.MenuType });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating menu item {MenuItemId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the menu item. Please try again.");
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating menu item {MenuItemId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadParentOptions(model, model.MenuType);
                await LoadDynamicOptions(model);
                return View("Form", model);
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVisible = !item.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Menu item {MenuItemId} visibility set to {IsVisible}", id, item.IsVisible);
                TempData["Success"] = $"'{item.MenuText}' " + (item.IsVisible ? "shown" : "hidden") + ".";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for menu item {MenuItemId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { menuType = item.MenuType });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();
            string mType = item.MenuType;

            bool hasChildren = await _context.MenuItems
                .AnyAsync(m => m.ParentId == id && !m.IsDeleted);

            if (hasChildren)
            {
                TempData["Error"] = $"Cannot delete '{item.MenuText}' — it has child items. Remove children first.";
                return RedirectToAction(nameof(Index), new { menuType = mType });
            }

            item.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Menu item {MenuItemId} '{MenuText}' deleted", id, item.MenuText);

                TempData["Success"] = $"'{item.MenuText}' deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting menu item {MenuItemId}", id);
                TempData["Error"] = "Unable to delete the item. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { menuType = mType });
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering menu item {MenuItemId}", id);
                TempData["Error"] = "Unable to reorder menu items. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { menuType = item.MenuType });
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

                "Club" => await _context.StudentClubs
                    .Where(c => c.IsVisible).OrderBy(c => c.Title)
                    .Select(c => new { id = c.Id, text = c.Title })
                    .ToListAsync<object>(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new { id = d.Id, text = d.Title })
                    .ToListAsync<object>(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
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
            List<MenuItem> all, int? parentId, int level, Func<MenuItem, string> resolveLabel)
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

        /// <summary>
        /// Walks up from <paramref name="proposedParentId"/> through its ancestor chain.
        /// Returns true if <paramref name="itemId"/> appears anywhere in that chain,
        /// which would mean assigning <paramref name="proposedParentId"/> as the parent
        /// of <paramref name="itemId"/> creates a cycle.
        /// </summary>
        private async Task<bool> WouldCreateCycleAsync(int itemId, int proposedParentId)
        {
            int? currentId = proposedParentId;
            var visited = new HashSet<int>();

            while (currentId.HasValue)
            {
                if (currentId.Value == itemId) return true;
                if (!visited.Add(currentId.Value)) break; // pre-existing cycle in data; stop rather than loop forever

                currentId = await _context.MenuItems
                    .Where(m => m.Id == currentId.Value)
                    .Select(m => m.ParentId)
                    .FirstOrDefaultAsync();
            }

            return false;
        }

        private async Task LoadParentOptions(MenuItemFormVM vm, string menuType)
        {
            var all = await _context.MenuItems
                .Where(m => m.MenuType == menuType && !m.IsDeleted && m.Id != vm.Id)
                .OrderBy(m => m.Position)
                .ToListAsync();

            var opts = new List<SelectListItem>
            {
                new() { Value = "", Text = "— Top Level (no parent) —" }
            };

            foreach (var top in all.Where(m => m.ParentId == null))
            {
                opts.Add(new SelectListItem { Value = top.Id.ToString(), Text = top.MenuText });
                foreach (var sub in all.Where(m => m.ParentId == top.Id))
                {
                    opts.Add(new SelectListItem { Value = sub.Id.ToString(), Text = $"  └─ {sub.MenuText}" });
                }
            }

            vm.ParentOptions = opts;
        }

        private async Task LoadDynamicOptions(MenuItemFormVM vm)
        {
            if (string.IsNullOrEmpty(vm.DynamicType) || vm.LinkType != "dynamic")
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

                "Club" => await _context.StudentClubs
                    .Where(c => c.IsVisible).OrderBy(c => c.Title)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title })
                    .ToListAsync(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible).OrderBy(d => d.Title)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Title })
                    .ToListAsync(),

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible).OrderBy(p => p.Title)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title })
                    .ToListAsync(),

                _ => new List<SelectListItem>()
            };
        }

        private void ValidateForm(MenuItemFormVM m)
        {
            if (m.LinkType == "internal" && string.IsNullOrWhiteSpace(m.ControllerName))
                ModelState.AddModelError(nameof(m.ControllerName), "Controller name is required for Internal links.");

            if (m.LinkType == "dynamic")
            {
                if (string.IsNullOrWhiteSpace(m.DynamicType))
                    ModelState.AddModelError(nameof(m.DynamicType), "Please select a dynamic type.");
                if (!m.DynamicId.HasValue)
                    ModelState.AddModelError(nameof(m.DynamicId), "Please select an item.");
            }

            if (m.LinkType == "external" && string.IsNullOrWhiteSpace(m.ExternalLink))
                ModelState.AddModelError(nameof(m.ExternalLink), "URL or PDF path is required for External links.");
        }

        private static MenuItem BuildEntity(MenuItemFormVM m) => new()
        {
            MenuText = m.MenuText,
            ParentId = m.ParentId == 0 ? null : m.ParentId,
            LinkType = m.LinkType,
            ControllerName = m.LinkType is "internal" or "dynamic" ? m.ControllerName : null,
            ActionName = m.LinkType is "internal" or "dynamic" ? m.ActionName : null,
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
            e.ControllerName = m.LinkType is "internal" or "dynamic" ? m.ControllerName : null;
            e.ActionName = m.LinkType is "internal" or "dynamic" ? m.ActionName : null;
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