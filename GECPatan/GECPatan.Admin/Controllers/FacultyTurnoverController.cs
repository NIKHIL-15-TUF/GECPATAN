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
        private readonly ILogger<FacultyTurnoverController> _logger;

        public FacultyTurnoverController(ApplicationDbContext context, ILogger<FacultyTurnoverController> logger)
        {
            _context = context;
            _logger = logger;
        }

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

            ViewBag.Departments = await GetActiveDepartmentSelectListAsync();
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

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            bool duplicate = await _context.FacultyTurnoverRecords.AnyAsync(t =>
                t.DeptId == model.DeptId &&
                t.AcademicYear == model.AcademicYear);
            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
                    "A record for this department and academic year already exists.");

            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            var record = new FacultyTurnoverRecord
            {
                DeptId = model.DeptId,
                AcademicYear = model.AcademicYear,
                NonTeachingJoin = model.NonTeachingJoin,
                TeachingJoin = model.TeachingJoin,
                NonTeachingLeft = model.NonTeachingLeft,
                TeachingLeft = model.TeachingLeft,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.FacultyTurnoverRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Faculty turnover record {RecordId} created for department {DeptId}, year {AcademicYear}",
                    record.Id, record.DeptId, record.AcademicYear);

                TempData["Success"] = "Faculty turnover record added.";
                return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error creating faculty turnover record for department {DeptId}, year {AcademicYear}",
                    model.DeptId, model.AcademicYear);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error creating faculty turnover record for department {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
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

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            bool duplicate = await _context.FacultyTurnoverRecords.AnyAsync(t =>
                t.Id != id &&
                t.DeptId == model.DeptId &&
                t.AcademicYear == model.AcademicYear);
            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
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

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Faculty turnover record {RecordId} updated", t.Id);

                TempData["Success"] = "Faculty turnover record updated.";
                return RedirectToAction(nameof(Index), new { deptId = t.DeptId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating faculty turnover record {RecordId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating faculty turnover record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating faculty turnover record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.FacultyTurnoverRecords.FindAsync(id);
            if (t == null) return NotFound();

            int deptId = t.DeptId;
            t.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Faculty turnover record {RecordId} soft-deleted", id);

                TempData["Success"] = "Record deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting faculty turnover record {RecordId}", id);
                TempData["Error"] = "Unable to delete the record. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task LoadDepts(FacultyTurnoverVM vm)
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