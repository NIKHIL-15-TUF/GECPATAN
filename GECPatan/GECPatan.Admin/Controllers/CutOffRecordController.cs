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
    public class CutoffRecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CutoffRecordController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX — grouped by department ─────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Cutoff Records — Mandatory Disclosure";

            var query = _context.CutoffRecords
                .Include(c => c.Department)
                .AsQueryable();

            if (deptId.HasValue)
                query = query.Where(c => c.DeptId == deptId.Value);

            var records = await query
                .OrderBy(c => c.Department!.DisplayOrder)
                .ThenByDescending(c => c.AcademicYear)
                .ToListAsync();

            var grouped = records
                .GroupBy(c => new { c.DeptId, Name = c.Department?.Name ?? "" })
                .Select(g => new CutoffRecordListVM
                {
                    DeptId = g.Key.DeptId,
                    DeptName = g.Key.Name,
                    Records = g.Select(c => new CutoffRecordRowVM
                    {
                        Id = c.Id,
                        AcademicYear = c.AcademicYear,
                        GeneralRank = c.GeneralRank,
                        SEBCRank = c.SEBCRank,
                        SCRank = c.SCRank,
                        STRank = c.STRank,
                        EWSRank = c.EWSRank,
                        IsVisible = c.IsVisible
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
            ViewData["Title"] = "Add Cutoff Record";
            var vm = new CutoffRecordVM
            {
                AcademicYear = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1) % 100:D2}"
            };
            if (deptId.HasValue) vm.DeptId = deptId.Value;

            await LoadDepartments(vm);
            return View("Form", vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CutoffRecordVM model)
        {
            ViewData["Title"] = "Add Cutoff Record";

            bool duplicate = await _context.CutoffRecords.AnyAsync(c =>
                c.DeptId == model.DeptId &&
                c.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError("AcademicYear",
                    "A cutoff record for this department and year already exists.");

            if (!ModelState.IsValid)
            {
                await LoadDepartments(model);
                return View("Form", model);
            }

            var record = new CutoffRecord
            {
                DeptId = model.DeptId,
                AcademicYear = model.AcademicYear,
                GeneralRank = model.GeneralRank,
                SEBCRank = model.SEBCRank,
                SCRank = model.SCRank,
                STRank = model.STRank,
                EWSRank = model.EWSRank,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            _context.CutoffRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cutoff record added.";
            return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Cutoff Record";
            var record = await _context.CutoffRecords.FindAsync(id);
            if (record == null) return NotFound();

            var vm = new CutoffRecordVM
            {
                Id = record.Id,
                DeptId = record.DeptId,
                AcademicYear = record.AcademicYear,
                GeneralRank = record.GeneralRank,
                SEBCRank = record.SEBCRank,
                SCRank = record.SCRank,
                STRank = record.STRank,
                EWSRank = record.EWSRank,
                DisplayOrder = record.DisplayOrder,
                IsVisible = record.IsVisible
            };
            await LoadDepartments(vm);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CutoffRecordVM model)
        {
            ViewData["Title"] = "Edit Cutoff Record";

            bool duplicate = await _context.CutoffRecords.AnyAsync(c =>
                c.Id != id &&
                c.DeptId == model.DeptId &&
                c.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError("AcademicYear",
                    "A cutoff record for this department and year already exists.");

            if (!ModelState.IsValid)
            {
                await LoadDepartments(model);
                return View("Form", model);
            }

            var record = await _context.CutoffRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.DeptId = model.DeptId;
            record.AcademicYear = model.AcademicYear;
            record.GeneralRank = model.GeneralRank;
            record.SEBCRank = model.SEBCRank;
            record.SCRank = model.SCRank;
            record.STRank = model.STRank;
            record.EWSRank = model.EWSRank;
            record.DisplayOrder = model.DisplayOrder;
            record.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cutoff record updated.";
            return RedirectToAction(nameof(Index), new { deptId = record.DeptId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.CutoffRecords.FindAsync(id);
            if (record == null) return NotFound();

            int deptId = record.DeptId;
            record.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cutoff record deleted.";
            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── HELPER ────────────────────────────────────────
        private async Task LoadDepartments(CutoffRecordVM vm)
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