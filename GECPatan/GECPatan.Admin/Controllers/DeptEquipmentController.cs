using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD")]
    public class DeptEquipmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeptEquipmentController> _logger;

        public DeptEquipmentController(ApplicationDbContext context, ILogger<DeptEquipmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX — list all depts with status ────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Department Equipment Lists";

            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            var equipmentsByDept = await _context.DepartmentEquipments
                .ToDictionaryAsync(e => e.DeptId);

            var vm = depts
                .Select(d =>
                {
                    equipmentsByDept.TryGetValue(d.DeptId, out var equipment);
                    return new DeptEquipmentListVM
                    {
                        DeptId = d.DeptId,
                        DeptName = d.Name,
                        HasContent = !string.IsNullOrWhiteSpace(equipment?.EquipmentHtml),
                        LastUpdated = equipment?.LastUpdated,
                        UpdatedBy = equipment?.UpdatedBy
                    };
                }).ToList();

            return View(vm);
        }

        // ── EDIT GET — TinyMCE per dept ───────────────────
        public async Task<IActionResult> Edit(int deptId)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DeptId == deptId);
            if (dept == null) return NotFound();

            ViewData["Title"] = $"Equipment — {dept.Name}";

            var existing = await _context.DepartmentEquipments
                .FirstOrDefaultAsync(e => e.DeptId == deptId);

            return View(new DeptEquipmentEditVM
            {
                Id = existing?.Id ?? 0,
                DeptId = deptId,
                DeptName = dept.Name,
                EquipmentHtml = existing?.EquipmentHtml
            });
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DeptEquipmentEditVM model)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DeptId == model.DeptId);
            if (dept == null) return NotFound();

            ViewData["Title"] = $"Equipment — {dept.Name}";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _context.DepartmentEquipments
                .FirstOrDefaultAsync(e => e.DeptId == model.DeptId);

            string userName = User.Identity?.Name ?? "Admin";

            try
            {
                if (existing == null)
                {
                    _context.DepartmentEquipments.Add(new DepartmentEquipment
                    {
                        DeptId = model.DeptId,
                        EquipmentHtml = model.EquipmentHtml,
                        LastUpdated = DateTime.Now,
                        UpdatedBy = userName
                    });
                }
                else
                {
                    existing.EquipmentHtml = model.EquipmentHtml;
                    existing.LastUpdated = DateTime.Now;
                    existing.UpdatedBy = userName;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Equipment list for department {DeptId} ({DeptName}) saved by {User}",
                    model.DeptId, dept.Name, userName);

                TempData["Success"] = $"Equipment list for {dept.Name} saved.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error saving equipment list for department {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "Unable to save the equipment list. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error saving equipment list for department {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }
    }
}