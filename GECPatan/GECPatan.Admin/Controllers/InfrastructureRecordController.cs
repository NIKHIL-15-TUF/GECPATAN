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
    public class InfrastructureRecordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InfrastructureRecordController> _logger;

        private static readonly string[] RoomTypeOptions =
        {
            "Classroom", "Tutorial Room", "Laboratory",
            "Drawing Hall", "Computer Center",
            "Library & Reading Room", "Seminar Hall", "Other"
        };

        public InfrastructureRecordController(
            ApplicationDbContext context,
            ILogger<InfrastructureRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Infrastructure Records";
            var records = await _context.InfrastructureRecords
                .Include(i => i.Department)
                .OrderBy(i => i.RoomType)
                .ThenBy(i => i.DisplayOrder)
                .ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Infrastructure Record";
            var vm = new InfrastructureRecordVM();
            await LoadLists(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InfrastructureRecordVM model)
        {
            ViewData["Title"] = "Add Infrastructure Record";

            if (model.DeptId.HasValue &&
                !await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId.Value))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!RoomTypeOptions.Contains(model.RoomType))
                ModelState.AddModelError(nameof(model.RoomType), "Please select a valid room type.");

            if (!ModelState.IsValid)
            {
                await LoadLists(model);
                return View("Form", model);
            }

            var record = new InfrastructureRecord
            {
                RoomType = model.RoomType,
                AreaSqm = model.AreaSqm,
                BuildingName = model.BuildingName,
                DeptId = model.DeptId,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.InfrastructureRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Infrastructure record {RecordId} created ({RoomType}, dept {DeptId})",
                    record.Id, record.RoomType, record.DeptId);

                TempData["Success"] = "Infrastructure record added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating infrastructure record ({RoomType})", model.RoomType);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadLists(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating infrastructure record");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadLists(model);
                return View("Form", model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Infrastructure Record";
            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();

            var vm = new InfrastructureRecordVM
            {
                Id = r.Id,
                RoomType = r.RoomType,
                AreaSqm = r.AreaSqm,
                BuildingName = r.BuildingName,
                DeptId = r.DeptId,
                DisplayOrder = r.DisplayOrder,
                IsVisible = r.IsVisible
            };
            await LoadLists(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InfrastructureRecordVM model)
        {
            ViewData["Title"] = "Edit Infrastructure Record";

            if (model.DeptId.HasValue &&
                !await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId.Value))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!RoomTypeOptions.Contains(model.RoomType))
                ModelState.AddModelError(nameof(model.RoomType), "Please select a valid room type.");

            if (!ModelState.IsValid)
            {
                await LoadLists(model);
                return View("Form", model);
            }

            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.RoomType = model.RoomType;
            r.AreaSqm = model.AreaSqm;
            r.BuildingName = model.BuildingName;
            r.DeptId = model.DeptId;
            r.DisplayOrder = model.DisplayOrder;
            r.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Infrastructure record {RecordId} updated", r.Id);

                TempData["Success"] = "Infrastructure record updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating infrastructure record {RecordId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                await LoadLists(model);
                return View("Form", model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating infrastructure record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                await LoadLists(model);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating infrastructure record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadLists(model);
                return View("Form", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Infrastructure record {RecordId} soft-deleted", id);

                TempData["Success"] = "Infrastructure record deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting infrastructure record {RecordId}", id);
                TempData["Error"] = "Unable to delete the record. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLists(InfrastructureRecordVM vm)
        {
            vm.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.RoomTypes = RoomTypeOptions
                .Select(t => new SelectListItem { Value = t, Text = t })
                .ToList();
        }
    }
}