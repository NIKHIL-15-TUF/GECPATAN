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
        private readonly ILogger<NBAAccreditationController> _logger;

        public NBAAccreditationController(ApplicationDbContext context, ILogger<NBAAccreditationController> logger)
        {
            _context = context;
            _logger = logger;
        }

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

            if (model.DeptId.HasValue &&
                !await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId.Value))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!ModelState.IsValid)
            {
                await LoadDepts(model);
                return View("Form", model);
            }

            var record = new NBAAccreditation
            {
                ProgramName = model.ProgramName,
                AccreditedBy = model.AccreditedBy,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                Status = model.Status,
                DeptId = model.DeptId,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.NBAAccreditations.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "NBA accreditation record {RecordId} created for program '{ProgramName}'",
                    record.Id, record.ProgramName);

                TempData["Success"] = "NBA accreditation record added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating NBA accreditation record for '{ProgramName}'", model.ProgramName);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating NBA accreditation record");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
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

            if (model.DeptId.HasValue &&
                !await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId.Value))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

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

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("NBA accreditation record {RecordId} updated", item.Id);

                TempData["Success"] = "NBA accreditation record updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating NBA accreditation record {RecordId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating NBA accreditation record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadDepts(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating NBA accreditation record {RecordId}", id);
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
            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            item.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("NBA accreditation record {RecordId} soft-deleted", id);

                TempData["Success"] = "Record deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting NBA accreditation record {RecordId}", id);
                TempData["Error"] = "Unable to delete the record. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.NBAAccreditations.FindAsync(id);
            if (item == null) return NotFound();

            item.IsVisible = !item.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("NBA accreditation record {RecordId} visibility set to {IsVisible}", id, item.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for NBA accreditation record {RecordId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

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