using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,Principal")]
    public class ProgramIntakeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgramIntakeController(ApplicationDbContext context)
            => _context = context;

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

            // HOD can only see their own dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var appUser = await _context.Users
                    .OfType<ApplicationUser>()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (appUser?.DeptId != deptId) return Forbid();
            }

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
            // Check duplicate year for same dept
            bool exists = await _context.ProgramIntakes
                .AnyAsync(p => p.DeptId == model.DeptId
                            && p.IntakeYear == model.IntakeYear);

            if (exists)
                ModelState.AddModelError("IntakeYear",
                    $"Intake for {model.IntakeYear} already exists for this department.");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix validation errors.";
                return RedirectToAction(nameof(Department),
                    new { deptId = model.DeptId });
            }

            _context.ProgramIntakes.Add(new ProgramIntake
            {
                DeptId = model.DeptId,
                IntakeYear = model.IntakeYear,
                Intake = model.Intake,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] =
                $"Intake {model.Intake} seats added for {model.IntakeYear}.";
            return RedirectToAction(nameof(Department),
                new { deptId = model.DeptId });
        }

        // ── EDIT (only latest year can be edited) ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int intake)
        {
            var record = await _context.ProgramIntakes.FindAsync(id);
            if (record == null) return NotFound();

            // Verify it's the latest year for this dept
            var latestYear = await _context.ProgramIntakes
                .Where(p => p.DeptId == record.DeptId)
                .MaxAsync(p => p.IntakeYear);

            if (record.IntakeYear < latestYear)
            {
                TempData["Error"] =
                    "Only the most recent year's intake can be edited.";
                return RedirectToAction(nameof(Department),
                    new { deptId = record.DeptId });
            }

            record.Intake = intake;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Intake updated to {intake} seats for {record.IntakeYear}.";
            return RedirectToAction(nameof(Department),
                new { deptId = record.DeptId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var record = await _context.ProgramIntakes.FindAsync(id);
            if (record == null) return NotFound();
            record.IsVisible = !record.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Department),
                new { deptId = record.DeptId });
        }
    }
}