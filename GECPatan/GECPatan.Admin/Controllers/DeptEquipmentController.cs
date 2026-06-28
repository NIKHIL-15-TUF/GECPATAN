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

        public DeptEquipmentController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX — list all depts with status ────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Department Equipment Lists";

            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            var equipments = await _context.DepartmentEquipments
                .ToListAsync();

            var vm = depts
                .Where(d => {
                    // Only show depts with intake (UG programs)
                    return true;
                })
                .Select(d => new DeptEquipmentListVM
                {
                    DeptId = d.DeptId ?? 0,
                    DeptName = d.Name,
                    HasContent = equipments.Any(e =>
                        e.DeptId == d.DeptId &&
                        !string.IsNullOrWhiteSpace(e.EquipmentHtml)),
                    LastUpdated = equipments
                        .FirstOrDefault(e => e.DeptId == d.DeptId)
                        ?.LastUpdated,
                    UpdatedBy = equipments
                        .FirstOrDefault(e => e.DeptId == d.DeptId)
                        ?.UpdatedBy
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

            var existing = await _context.DepartmentEquipments
                .FirstOrDefaultAsync(e => e.DeptId == model.DeptId);

            string userName = User.Identity?.Name ?? "Admin";

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
            TempData["Success"] = $"Equipment list for {dept.Name} saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
