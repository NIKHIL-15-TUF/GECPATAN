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
        private readonly ILogger<CutoffRecordController> _logger;

        public CutoffRecordController(ApplicationDbContext context, ILogger<CutoffRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

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

            ViewBag.Departments = await GetActiveDepartmentSelectListAsync();
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

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            bool duplicate = await _context.CutoffRecords.AnyAsync(c =>
                c.DeptId == model.DeptId &&
                c.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
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

            try
            {
                _context.CutoffRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cutoff record {CutoffRecordId} created for department {DeptId}, year {AcademicYear}",
                    record.Id, record.DeptId, record.AcademicYear);

                TempData["Success"] = "Cutoff record added.";
                return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error while creating cutoff record for department {DeptId}, year {AcademicYear}",
                    model.DeptId, model.AcademicYear);
                ModelState.AddModelError(string.Empty, "Unable to save the cutoff record. Please try again.");
                await LoadDepartments(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while creating cutoff record for department {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepartments(model);
                return View("Form", model);
            }
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

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            bool duplicate = await _context.CutoffRecords.AnyAsync(c =>
                c.Id != id &&
                c.DeptId == model.DeptId &&
                c.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
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

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cutoff record {CutoffRecordId} updated", record.Id);

                TempData["Success"] = "Cutoff record updated.";
                return RedirectToAction(nameof(Index), new { deptId = record.DeptId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating cutoff record {CutoffRecordId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                await LoadDepartments(model);
                return View("Form", model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating cutoff record {CutoffRecordId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the cutoff record. Please try again.");
                await LoadDepartments(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating cutoff record {CutoffRecordId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepartments(model);
                return View("Form", model);
            }
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.CutoffRecords.FindAsync(id);
            if (record == null) return NotFound();

            int deptId = record.DeptId;
            record.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cutoff record {CutoffRecordId} soft-deleted", id);

                TempData["Success"] = "Cutoff record deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting cutoff record {CutoffRecordId}", id);
                TempData["Error"] = "Unable to delete the cutoff record. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting cutoff record {CutoffRecordId}", id);
                TempData["Error"] = "An unexpected error occurred. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task LoadDepartments(CutoffRecordVM vm)
        {
            vm.Departments = await GetActiveDepartmentSelectListAsync();
        }

        private Task<List<SelectListItem>> GetActiveDepartmentSelectListAsync() =>
            _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();
    }
}