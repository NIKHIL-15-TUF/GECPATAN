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
        {
            _context = context;
        }

        // ── INDEX — Tree View ─────────────────────────────
        public async Task<IActionResult> Index(string menuType = "Main")
        {
            ViewData["Title"] = menuType == "Footer"
                ? "Footer Menu" : "Navigation Menu";
            ViewBag.MenuType = menuType;
            ViewBag.IsFooter = menuType == "Footer";

            var allItems = await _context.MenuItems
                .Where(m => m.MenuType == menuType)
                .OrderBy(m => m.Position)
                .ToListAsync();

            // Build tree
            var tree = BuildTree(allItems, null);
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
            await PopulateDropdowns(vm, menuType);
            return View(vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemFormVM model)
        {
            ViewData["Title"] = "Add Menu Item";
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model, model.MenuType);
                return View(model);
            }

            // Get max position for same parent
            int maxPos = await _context.MenuItems
                .Where(m => m.ParentId == model.ParentId &&
                            m.MenuType == model.MenuType)
                .Select(m => (int?)m.Position).MaxAsync() ?? -1;

            var item = MapFormToEntity(model);
            item.Position = maxPos + 1;

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Menu item '{item.MenuText}' added.";
            return RedirectToAction(nameof(Index),
                new { menuType = model.MenuType });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Menu Item";
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var vm = MapEntityToForm(item);
            await PopulateDropdowns(vm, item.MenuType);
            return View(vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemFormVM model)
        {
            ViewData["Title"] = "Edit Menu Item";
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model, model.MenuType);
                return View(model);
            }

            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            // Prevent circular parent (item cannot be its own parent)
            if (model.ParentId == id)
            {
                ModelState.AddModelError("ParentId",
                    "An item cannot be its own parent.");
                await PopulateDropdowns(model, model.MenuType);
                return View(model);
            }

            UpdateEntityFromForm(item, model);
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
            return RedirectToAction(nameof(Index),
                new { menuType = item.MenuType });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            // Check for children
            var hasChildren = await _context.MenuItems
                .AnyAsync(m => m.ParentId == id);

            if (hasChildren)
            {
                TempData["Error"] =
                    "Cannot delete — this item has child items. " +
                    "Delete or move children first.";
                return RedirectToAction(nameof(Index),
                    new { menuType = item.MenuType });
            }

            string menuType = item.MenuType;
            item.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{item.MenuText}' deleted.";
            return RedirectToAction(nameof(Index),
                new { menuType });
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var siblings = await _context.MenuItems
                .Where(m => m.ParentId == item.ParentId &&
                            m.MenuType == item.MenuType &&
                            !m.IsDeleted)
                .OrderBy(m => m.Position)
                .ToListAsync();

            int idx = siblings.FindIndex(m => m.Id == id);

            if (direction == "up" && idx > 0)
            {
                siblings[idx].Position--;
                siblings[idx - 1].Position++;
            }
            else if (direction == "down" && idx < siblings.Count - 1)
            {
                siblings[idx].Position++;
                siblings[idx + 1].Position--;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { menuType = item.MenuType });
        }

        // ── GET DYNAMIC IDs (AJAX) ────────────────────────
        // Returns IDs for selected dynamic type
        [HttpGet]
        public async Task<IActionResult> GetDynamicIds(string type)
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

                "ContentPage" => await _context.ContentPages
                    .Where(p => p.IsVisible)
                    .OrderBy(p => p.Title)
                    .Select(p => new { id = p.Id, text = p.Title })
                    .ToListAsync<object>(),

                "Document" => await _context.DocumentCategories
                    .Where(d => d.IsVisible)
                    .OrderBy(d => d.Title)
                    .Select(d => new { id = d.Id, text = d.Title })
                    .ToListAsync<object>(),

                _ => new List<object>()
            };

            return Json(items);
        }

        // ══════════════════════════════════════════════════
        // TREE BUILDER
        // ══════════════════════════════════════════════════
        private static List<MenuItemTreeVM> BuildTree(
            List<MenuItem> allItems, int? parentId, int level = 0)
        {
            return allItems
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.Position)
                .Select(m => new MenuItemTreeVM
                {
                    Id = m.Id,
                    MenuText = m.MenuText,
                    ParentId = m.ParentId,
                    ControllerName = m.ControllerName,
                    ActionName = m.ActionName,
                    DynamicId = m.DynamicId,
                    DynamicType = m.DynamicType,
                    Link = m.Link,
                    CssClass = m.CssClass,
                    MenuType = m.MenuType,
                    Position = m.Position,
                    IsVisible = m.IsVisible,
                    OpenInNewTab = m.OpenInNewTab,
                    Level = level,
                    Children = BuildTree(allItems, m.Id, level + 1)
                }).ToList();
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private async Task PopulateDropdowns(
            MenuItemFormVM vm, string menuType)
        {
            // Parent items — only Level 1 and Level 2 can be parents
            // (max 3 levels deep)
            var allItems = await _context.MenuItems
                .Where(m => m.MenuType == menuType && !m.IsDeleted)
                .OrderBy(m => m.Position)
                .ToListAsync();

            // Build flat list with indentation
            var parentOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "— Top Level (No Parent) —" }
            };

            foreach (var top in allItems.Where(m => m.ParentId == null)
                .OrderBy(m => m.Position))
            {
                parentOptions.Add(new SelectListItem
                {
                    Value = top.Id.ToString(),
                    Text = top.MenuText
                });

                // Level 2 items can also be parents (for 3-level menus)
                foreach (var child in allItems
                    .Where(m => m.ParentId == top.Id)
                    .OrderBy(m => m.Position))
                {
                    parentOptions.Add(new SelectListItem
                    {
                        Value = child.Id.ToString(),
                        Text = $"    └─ {child.MenuText}"
                    });
                }
            }

            vm.ParentItems = parentOptions;

            // Dynamic dropdowns
            vm.DepartmentItems = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.CommitteeItems = await _context.CampusCommittees
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Title
                }).ToListAsync();

            vm.FacilityItems = await _context.Facilities
                .Where(f => f.IsActive)
                .OrderBy(f => f.Title)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Title
                }).ToListAsync();

            vm.ClubItems = await _context.StudentClubs
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Title
                }).ToListAsync();

            vm.ContentPageItems = await _context.ContentPages
                .Where(p => p.IsVisible)
                .OrderBy(p => p.Title)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                }).ToListAsync();

            vm.DocumentCatItems = await _context.DocumentCategories
                .Where(d => d.IsVisible)
                .OrderBy(d => d.Title)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Title
                }).ToListAsync();
        }

        private static MenuItem MapFormToEntity(MenuItemFormVM vm)
        {
            var item = new MenuItem
            {
                MenuText = vm.MenuText,
                ParentId = vm.ParentId,
                MenuType = vm.MenuType,
                IsVisible = vm.IsVisible,
                Position = vm.Position,
                CssClass = vm.CssClass,
                OpenInNewTab = vm.OpenInNewTab
            };
            ApplyLinkType(item, vm);
            return item;
        }

        private static void UpdateEntityFromForm(
            MenuItem item, MenuItemFormVM vm)
        {
            item.MenuText = vm.MenuText;
            item.ParentId = vm.ParentId;
            item.MenuType = vm.MenuType;
            item.IsVisible = vm.IsVisible;
            item.Position = vm.Position;
            item.CssClass = vm.CssClass;
            item.OpenInNewTab = vm.OpenInNewTab;
            ApplyLinkType(item, vm);
        }

        private static void ApplyLinkType(MenuItem item, MenuItemFormVM vm)
        {
            switch (vm.LinkType)
            {
                case "internal":
                    item.ControllerName = vm.ControllerName;
                    item.ActionName = vm.ActionName;
                    item.DynamicId = null;
                    item.DynamicType = null;
                    item.Link = null;
                    break;

                case "dynamic":
                    item.ControllerName = vm.ControllerName;
                    item.ActionName = vm.ActionName;
                    item.DynamicType = vm.DynamicType;
                    item.DynamicId = vm.DynamicId;
                    item.Link = null;
                    break;

                case "external":
                    item.Link = vm.Link;
                    item.ControllerName = null;
                    item.ActionName = null;
                    item.DynamicId = null;
                    item.DynamicType = null;
                    break;

                case "none":
                default:
                    item.ControllerName = null;
                    item.ActionName = null;
                    item.DynamicId = null;
                    item.DynamicType = null;
                    item.Link = null;
                    break;
            }
        }

        private static MenuItemFormVM MapEntityToForm(MenuItem item)
        {
            // Detect link type
            string linkType = "none";
            if (!string.IsNullOrEmpty(item.Link))
                linkType = "external";
            else if (item.DynamicId.HasValue)
                linkType = "dynamic";
            else if (!string.IsNullOrEmpty(item.ControllerName))
                linkType = "internal";

            return new MenuItemFormVM
            {
                Id = item.Id,
                MenuText = item.MenuText,
                ParentId = item.ParentId,
                LinkType = linkType,
                ControllerName = item.ControllerName,
                ActionName = item.ActionName,
                DynamicType = item.DynamicType,
                DynamicId = item.DynamicId,
                Link = item.Link,
                CssClass = item.CssClass,
                MenuType = item.MenuType,
                Position = item.Position,
                IsVisible = item.IsVisible,
                OpenInNewTab = item.OpenInNewTab
            };
        }
    }
}