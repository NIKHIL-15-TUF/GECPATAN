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
    public class NBAAccreditationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NBAAccreditationController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "NBA Accreditation Records";

            var items = await _context.NBAAccreditations
                .Include(n => n.Department)
                .Where(n => !n.IsDeleted)
                .OrderBy(n => n.DisplayOrder)
                .ToListAsync();

            return View(items);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add NBA Accreditation";
            var vm = new NBAAccreditationVM();
            await LoadDepts(vm);
            return View("Form", vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NBAAccreditationVM model)
        {
            ViewData["Title"] = "Add NBA Accreditation";
            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            _context.NBAAccreditations.Add(new NBAAccreditation
            {
                ProgramName = model.ProgramName,
                AccreditedBy = model.AccreditedBy,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                Status = model.Status,
                DeptId = model.DeptId,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "NBA accreditation record added.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit NBA Accreditation";
            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            var vm = new NBAAccreditationVM
            {
                Id = item.Id,
                ProgramName = item.ProgramName,
                AccreditedBy = item.AccreditedBy,
                ValidFrom = item.ValidFrom,
                ValidTo = item.ValidTo,
                Status = item.Status,
                DeptId = item.DeptId,
                DisplayOrder = item.DisplayOrder,
                IsVisible = item.IsVisible
            };
            await LoadDepts(vm);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NBAAccreditationVM model)
        {
            ViewData["Title"] = "Edit NBA Accreditation";
            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            item.ProgramName = model.ProgramName;
            item.AccreditedBy = model.AccreditedBy;
            item.ValidFrom = model.ValidFrom;
            item.ValidTo = model.ValidTo;
            item.Status = model.Status;
            item.DeptId = model.DeptId;
            item.DisplayOrder = model.DisplayOrder;
            item.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "NBA accreditation record updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Record deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVisible = !item.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDepts(NBAAccreditationVM vm)
        {
            vm.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();
        }
    }
}
