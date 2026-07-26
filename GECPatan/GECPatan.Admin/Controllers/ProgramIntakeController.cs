using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,Principal")]
    public class ProgramIntakeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProgramIntakeController> _logger;

        public ProgramIntakeController(ApplicationDbContext context, ILogger<ProgramIntakeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX — all depts with their latest intake ────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Program Intake";

            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            var allIntakes = await _context.ProgramIntakes
                .OrderByDescending(p => p.IntakeYear)
                .ToListAsync();

            var list = depts.Select(d =>
            {
                var deptIntakes = allIntakes
                    .Where(p => p.DeptId == d.DeptId)
                    .OrderByDescending(p => p.IntakeYear)
                    .ToList();

                var latest = deptIntakes.FirstOrDefault();

                return new ProgramIntakeIndexVM
                {
                    DeptId = (int)d.DeptId,
                    DeptName = d.Name,
                    LatestIntake = latest?.Intake ?? 0,
                    LatestYear = latest?.IntakeYear ?? 0,
                    Intakes = deptIntakes.Select(p => new ProgramIntakeRowVM
                    {
                        Id = p.Id,
                        DeptId = p.DeptId,
                        DeptName = d.Name,
                        IntakeYear = p.IntakeYear,
                        Intake = p.Intake,
                        IsVisible = p.IsVisible,
                        IsLatest = p == deptIntakes.First()
                    }).ToList()
                };
            }).ToList();

            return View(list);
        }

        // ── DEPT DETAIL — year-wise history for one dept ──
        public async Task<IActionResult> Department(int deptId)
        {
            ViewData["Title"] = "Intake History";

            var dept = await _context.Departments.FindAsync(deptId);
            if (dept == null) return NotFound();

            if (!await CanAccessDeptAsync(deptId))
                return Forbid();

            var intakes = await _context.ProgramIntakes
                .Where(p => p.DeptId == deptId)
                .OrderByDescending(p => p.IntakeYear)
                .ToListAsync();

            var vm = new ProgramIntakeIndexVM
            {
                DeptId = (int)dept.DeptId,
                DeptName = dept.Name,
                LatestIntake = intakes.FirstOrDefault()?.Intake ?? 0,
                LatestYear = intakes.FirstOrDefault()?.IntakeYear ?? 0,
                Intakes = intakes.Select((p, idx) => new ProgramIntakeRowVM
                {
                    Id = p.Id,
                    DeptId = p.DeptId,
                    DeptName = dept.Name,
                    IntakeYear = p.IntakeYear,
                    Intake = p.Intake,
                    IsVisible = p.IsVisible,
                    IsLatest = idx == 0
                }).ToList()
            };

            ViewBag.AddVM = new ProgramIntakeFormVM
            {
                DeptId = deptId,
                IntakeYear = DateTime.Now.Year,
                Intake = intakes.FirstOrDefault()?.Intake ?? 60
            };

            return View(vm);
        }

        // ── ADD INTAKE (per dept) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProgramIntakeFormVM model)
        {
            if (!await CanAccessDeptAsync(model.DeptId))
                return Forbid();

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            bool exists = await _context.ProgramIntakes
                .AnyAsync(p => p.DeptId == model.DeptId && p.IntakeYear == model.IntakeYear);

            if (exists)
                ModelState.AddModelError(nameof(model.IntakeYear),
                    $"Intake for {model.IntakeYear} already exists for this department.");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix validation errors.";
                return RedirectToAction(nameof(Department), new { deptId = model.DeptId });
            }

            var record = new ProgramIntake
            {
                DeptId = model.DeptId,
                IntakeYear = model.IntakeYear,
                Intake = model.Intake,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.ProgramIntakes.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Program intake {RecordId} added for dept {DeptId}, year {IntakeYear} ({Intake} seats)",
                    record.Id, record.DeptId, record.IntakeYear, record.Intake);

                TempData["Success"] = $"Intake {model.Intake} seats added for {model.IntakeYear}.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error adding program intake for dept {DeptId}, year {IntakeYear}",
                    model.DeptId, model.IntakeYear);
                TempData["Error"] = "Unable to add the intake record. Please try again.";
            }

            return RedirectToAction(nameof(Department), new { deptId = model.DeptId });
        }

        // ── EDIT (only latest year can be edited) ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int intake)
        {
            var record = await _context.ProgramIntakes.FindAsync(id);
            if (record == null) return NotFound();

            if (!await CanAccessDeptAsync(record.DeptId))
                return Forbid();

            var latestYear = await _context.ProgramIntakes
                .Where(p => p.DeptId == record.DeptId)
                .MaxAsync(p => p.IntakeYear);

            if (record.IntakeYear < latestYear)
            {
                TempData["Error"] = "Only the most recent year's intake can be edited.";
                return RedirectToAction(nameof(Department), new { deptId = record.DeptId });
            }

            record.Intake = intake;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Program intake {RecordId} updated to {Intake} seats", id, intake);

                TempData["Success"] = $"Intake updated to {intake} seats for {record.IntakeYear}.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating program intake {RecordId}", id);
                TempData["Error"] = "This record was changed by someone else. Please reload and try again.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating program intake {RecordId}", id);
                TempData["Error"] = "Unable to update the intake. Please try again.";
            }

            return RedirectToAction(nameof(Department), new { deptId = record.DeptId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var record = await _context.ProgramIntakes.FindAsync(id);
            if (record == null) return NotFound();

            if (!await CanAccessDeptAsync(record.DeptId))
                return Forbid();

            record.IsVisible = !record.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Program intake {RecordId} visibility set to {IsVisible}", id, record.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for program intake {RecordId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Department), new { deptId = record.DeptId });
        }

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// SuperAdmin and Principal can access any department's intake data.
        /// HOD is restricted to their own department, matching the check
        /// already present in the original Department GET action.
        /// </summary>
        private async Task<bool> CanAccessDeptAsync(int deptId)
        {
            if (!User.IsInRole(AppRoles.HOD)) return true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return appUser?.DeptId == deptId;
        }
    }
}