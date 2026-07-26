using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class ScholarshipRecordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScholarshipRecordController> _logger;

        public ScholarshipRecordController(ApplicationDbContext context, ILogger<ScholarshipRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Scholarship Records";

            var records = await _context.ScholarshipRecords
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ScholarshipRecordVM
                {
                    Id = x.Id,
                    SchemeName = x.SchemeName,
                    AcademicYear = x.AcademicYear,
                    TotalApplications = x.TotalApplications,
                    DisplayOrder = x.DisplayOrder,
                    IsVisible = x.IsVisible
                })
                .ToListAsync();

            return View(records);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Scholarship Record";
            return View("Form", new ScholarshipRecordVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScholarshipRecordVM model)
        {
            ViewData["Title"] = "Add Scholarship Record";

            bool duplicate = await _context.ScholarshipRecords.AnyAsync(x =>
                x.SchemeName == model.SchemeName && x.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
                    "A record for this scheme and academic year already exists.");

            if (!ModelState.IsValid) return View("Form", model);

            var record = new ScholarshipRecord
            {
                SchemeName = model.SchemeName,
                AcademicYear = model.AcademicYear,
                TotalApplications = model.TotalApplications,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.ScholarshipRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Scholarship record {RecordId} created for scheme '{SchemeName}', year {AcademicYear}",
                    record.Id, record.SchemeName, record.AcademicYear);

                TempData["Success"] = "Scholarship record added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating scholarship record for '{SchemeName}'", model.SchemeName);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating scholarship record for '{SchemeName}'", model.SchemeName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View("Form", model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Scholarship Record";
            var r = await _context.ScholarshipRecords.FindAsync(id);
            if (r == null) return NotFound();

            var vm = new ScholarshipRecordVM
            {
                Id = r.Id,
                SchemeName = r.SchemeName,
                AcademicYear = r.AcademicYear,
                TotalApplications = r.TotalApplications,
                DisplayOrder = r.DisplayOrder,
                IsVisible = r.IsVisible
            };
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScholarshipRecordVM model)
        {
            ViewData["Title"] = "Edit Scholarship Record";

            bool duplicate = await _context.ScholarshipRecords.AnyAsync(x =>
                x.Id != id && x.SchemeName == model.SchemeName && x.AcademicYear == model.AcademicYear);

            if (duplicate)
                ModelState.AddModelError(nameof(model.AcademicYear),
                    "A record for this scheme and academic year already exists.");

            if (!ModelState.IsValid) return View("Form", model);

            var r = await _context.ScholarshipRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.SchemeName = model.SchemeName;
            r.AcademicYear = model.AcademicYear;
            r.TotalApplications = model.TotalApplications;
            r.DisplayOrder = model.DisplayOrder;
            r.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Scholarship record {RecordId} updated", r.Id);

                TempData["Success"] = "Scholarship record updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating scholarship record {RecordId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                return View("Form", model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating scholarship record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the record. Please try again.");
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating scholarship record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View("Form", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.ScholarshipRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Scholarship record {RecordId} soft-deleted", id);

                TempData["Success"] = "Scholarship record deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting scholarship record {RecordId}", id);
                TempData["Error"] = "Unable to delete the record. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}