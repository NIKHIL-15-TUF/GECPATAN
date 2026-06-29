using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    public class DisclosurePlacementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisclosurePlacementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── INDEX ─────────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Placement Records — Mandatory Disclosure";

            var query = _context.DisclosurePlacements
                .Include(p => p.Department)
                .AsQueryable();

            if (deptId.HasValue)
                query = query.Where(p => p.DeptId == deptId.Value);

            var records = await query
                .OrderBy(p => p.Department!.DisplayOrder)
                .ThenByDescending(p => p.AcademicYear)
                .ToListAsync();

            var grouped = records
                .GroupBy(p => new { p.DeptId, Name = p.Department!.Name })
                .Select(g => new DisclosurePlacementListVM
                {
                    DeptId = g.Key.DeptId,
                    DeptName = g.Key.Name,
                    Records = g.Select(p => new DisclosurePlacementRowVM
                    {
                        Id = p.Id,
                        DepartmentName = p.Department!.Name,
                        AcademicYear = p.AcademicYear,
                        NoOfCompanies = p.NoOfCompanies,
                        TotalPlaced = p.TotalPlaced,
                        MaximumSalary = p.MaximumSalary,
                        MinimumSalary = p.MinimumSalary,
                        DeptId = p.DeptId,
                        DisplayOrder = p.DisplayOrder,
                        IsVisible = p.IsVisible
                    }).ToList()
                })
                .OrderBy(g => g.DeptName)
                .ToList();

            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            ViewBag.SelectedDeptId = deptId;

            return View(grouped);
        }

        // ── CREATE GET ───────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Add Placement Record";

            var vm = new DisclosurePlacementRowVM
            {
                AcademicYear = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1) % 100:D2}"
            };

            if (deptId.HasValue)
                vm.DeptId = deptId.Value;

            await LoadDepartments(vm);

            return View("Form", vm);
        }

        // ── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisclosurePlacementRowVM model)
        {
            ViewData["Title"] = "Add Placement Record";

            bool duplicate = await _context.DisclosurePlacements.AnyAsync(p =>
                p.DeptId == model.DeptId &&
                p.AcademicYear == model.AcademicYear);

            if (duplicate)
            {
                ModelState.AddModelError("AcademicYear",
                    "A placement record for this department and year already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDepartments(model);
                return View("Form", model);
            }

            var record = new DisclosurePlacementData
            {
                DeptId = model.DeptId ?? 0,
                AcademicYear = model.AcademicYear,
                NoOfCompanies = model.NoOfCompanies,
                TotalPlaced = model.TotalPlaced,
                MaximumSalary = model.MaximumSalary,
                MinimumSalary = model.MinimumSalary,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            _context.DisclosurePlacements.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Placement record added successfully.";

            return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
        }

        // ── EDIT GET ─────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Placement Record";

            var record = await _context.DisclosurePlacements.FindAsync(id);

            if (record == null)
                return NotFound();

            var vm = new DisclosurePlacementRowVM
            {
                Id = record.Id,
                DeptId = record.DeptId,
                DepartmentName = record.Department?.Name,
                AcademicYear = record.AcademicYear,
                NoOfCompanies = record.NoOfCompanies,
                TotalPlaced = record.TotalPlaced,
                MaximumSalary = record.MaximumSalary,
                MinimumSalary = record.MinimumSalary,
                DisplayOrder = record.DisplayOrder,
                IsVisible = record.IsVisible
            };

            await LoadDepartments(vm);

            return View("Form", vm);
        }

        // ── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DisclosurePlacementRowVM model)
        {
            ViewData["Title"] = "Edit Placement Record";

            bool duplicate = await _context.DisclosurePlacements.AnyAsync(p =>
                p.Id != id &&
                p.DeptId == model.DeptId &&
                p.AcademicYear == model.AcademicYear);

            if (duplicate)
            {
                ModelState.AddModelError("AcademicYear",
                    "A placement record for this department and year already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDepartments(model);
                return View("Form", model);
            }

            var record = await _context.DisclosurePlacements.FindAsync(id);

            if (record == null)
                return NotFound();

            record.DeptId = model.DeptId ?? 0;
            record.AcademicYear = model.AcademicYear;
            record.NoOfCompanies = model.NoOfCompanies;
            record.TotalPlaced = model.TotalPlaced;
            record.MaximumSalary = model.MaximumSalary;
            record.MinimumSalary = model.MinimumSalary;
            record.DisplayOrder = model.DisplayOrder;
            record.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Placement record updated successfully.";

            return RedirectToAction(nameof(Index), new { deptId = record.DeptId });
        }

        // ── DELETE ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.DisclosurePlacements.FindAsync(id);

            if (record == null)
                return NotFound();

            int deptId = record.DeptId;

            record.IsDeleted = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Placement record deleted successfully.";

            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── HELPER ───────────────────────────────────────────
        private async Task LoadDepartments(DisclosurePlacementRowVM vm)
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