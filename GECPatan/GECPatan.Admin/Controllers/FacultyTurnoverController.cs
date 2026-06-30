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
    public class FacultyTurnoverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacultyTurnoverController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX — grouped by department ─────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Faculty Turnover (Join/Left) — Mandatory Disclosure";

            var query = _context.FacultyTurnoverRecords
                .Include(t => t.Department)
                .AsQueryable();

            if (deptId.HasValue)
                query = query.Where(t => t.DeptId == deptId.Value);

            var records = await query
                .OrderBy(t => t.Department!.DisplayOrder)
                .ThenByDescending(t => t.AcademicYear)
                .ToListAsync();

            var grouped = records
                .GroupBy(t => new { t.DeptId, Name = t.Department?.Name ?? "" })
                .Select(g => new FacultyTurnoverGroupVM
                {
                    DeptId = g.Key.DeptId,
                    DeptName = g.Key.Name,
                    Records = g.Select(t => new FacultyTurnoverVM
                    {
                        Id = t.Id,
                        DeptId = t.DeptId,
                        AcademicYear = t.AcademicYear,
                        NonTeachingJoin = t.NonTeachingJoin,
                        TeachingJoin = t.TeachingJoin,
                        NonTeachingLeft = t.NonTeachingLeft,
                        TeachingLeft = t.TeachingLeft,
                        DisplayOrder = t.DisplayOrder,
                        IsVisible = t.IsVisible
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

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Add Faculty Turnover Record";
            var vm = new FacultyTurnoverVM
            {
                AcademicYear = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1) % 100:D2}"
            };
            if (deptId.HasValue) vm.DeptId = deptId.Value;

            await LoadDepts(vm);
            return View("Form", vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FacultyTurnoverVM model)
        {
            ViewData["Title"] = "Add Faculty Turnover Record";

            bool duplicate = await _context.FacultyTurnoverRecords.AnyAsync(t =>
                t.DeptId == model.DeptId &&
                t.AcademicYear == model.AcademicYear);
            if (duplicate)
                ModelState.AddModelError("AcademicYear",
                    "A record for this department and academic year already exists.");

            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            _context.FacultyTurnoverRecords.Add(new FacultyTurnoverRecord
            {
                DeptId = model.DeptId,
                AcademicYear = model.AcademicYear,
                NonTeachingJoin = model.NonTeachingJoin,
                TeachingJoin = model.TeachingJoin,
                NonTeachingLeft = model.NonTeachingLeft,
                TeachingLeft = model.TeachingLeft,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Faculty turnover record added.";
            return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Faculty Turnover Record";
            var t = await _context.FacultyTurnoverRecords.FindAsync(id);
            if (t == null) return NotFound();

            var vm = new FacultyTurnoverVM
            {
                Id = t.Id,
                DeptId = t.DeptId,
                AcademicYear = t.AcademicYear,
                NonTeachingJoin = t.NonTeachingJoin,
                TeachingJoin = t.TeachingJoin,
                NonTeachingLeft = t.NonTeachingLeft,
                TeachingLeft = t.TeachingLeft,
                DisplayOrder = t.DisplayOrder,
                IsVisible = t.IsVisible
            };
            await LoadDepts(vm);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacultyTurnoverVM model)
        {
            ViewData["Title"] = "Edit Faculty Turnover Record";

            bool duplicate = await _context.FacultyTurnoverRecords.AnyAsync(t =>
                t.Id != id &&
                t.DeptId == model.DeptId &&
                t.AcademicYear == model.AcademicYear);
            if (duplicate)
                ModelState.AddModelError("AcademicYear",
                    "A record for this department and academic year already exists.");

            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            var t = await _context.FacultyTurnoverRecords.FindAsync(id);
            if (t == null) return NotFound();

            t.DeptId = model.DeptId;
            t.AcademicYear = model.AcademicYear;
            t.NonTeachingJoin = model.NonTeachingJoin;
            t.TeachingJoin = model.TeachingJoin;
            t.NonTeachingLeft = model.NonTeachingLeft;
            t.TeachingLeft = model.TeachingLeft;
            t.DisplayOrder = model.DisplayOrder;
            t.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Faculty turnover record updated.";
            return RedirectToAction(nameof(Index), new { deptId = t.DeptId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.FacultyTurnoverRecords.FindAsync(id);
            if (t == null) return NotFound();

            int deptId = t.DeptId;
            t.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Record deleted.";
            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── HELPER ────────────────────────────────────────
        private async Task LoadDepts(FacultyTurnoverVM vm)
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
