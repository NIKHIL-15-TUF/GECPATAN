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

        public ScholarshipRecordController(ApplicationDbContext context)
            => _context = context;

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
            if (!ModelState.IsValid) return View("Form", model);

            _context.ScholarshipRecords.Add(new ScholarshipRecord
            {
                SchemeName = model.SchemeName,
                AcademicYear = model.AcademicYear,
                TotalApplications = model.TotalApplications,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Scholarship record added.";
            return RedirectToAction(nameof(Index));
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
            if (!ModelState.IsValid) return View("Form", model);

            var r = await _context.ScholarshipRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.SchemeName = model.SchemeName;
            r.AcademicYear = model.AcademicYear;
            r.TotalApplications = model.TotalApplications;
            r.DisplayOrder = model.DisplayOrder;
            r.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Scholarship record updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.ScholarshipRecords.FindAsync(id);
            if (r == null) return NotFound();
            r.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Scholarship record deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}