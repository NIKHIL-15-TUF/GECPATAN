using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principal")]
    public class ProgramIntakeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgramIntakeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Program Intake";
            var items = await _context.ProgramIntakes
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Program";
            return View(new ProgramIntakeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProgramIntakeVM model)
        {
            ViewData["Title"] = "Add Program";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.ProgramIntakes
                .Select(p => (int?)p.DisplayOrder).MaxAsync() ?? -1;

            _context.ProgramIntakes.Add(new ProgramIntake
            {
                ProgramName = model.ProgramName,
                Intake = model.Intake,
                CourseCode = model.CourseCode,
                DisplayOrder = maxOrder + 1
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Program added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Program";
            var p = await _context.ProgramIntakes.FindAsync(id);
            if (p == null) return NotFound();

            return View(new ProgramIntakeVM
            {
                Id = p.Id,
                ProgramName = p.ProgramName,
                Intake = p.Intake,
                CourseCode = p.CourseCode,
                DisplayOrder = p.DisplayOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProgramIntakeVM model)
        {
            ViewData["Title"] = "Edit Program";
            if (!ModelState.IsValid) return View(model);

            var p = await _context.ProgramIntakes.FindAsync(id);
            if (p == null) return NotFound();

            p.ProgramName = model.ProgramName;
            p.Intake = model.Intake;
            p.CourseCode = model.CourseCode;
            p.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Program updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.ProgramIntakes.FindAsync(id);
            if (p == null) return NotFound();
            p.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Program deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var p = await _context.ProgramIntakes.FindAsync(id);
            if (p == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.ProgramIntakes
                    .Where(x => x.DisplayOrder == p.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; p.DisplayOrder--; }
            }
            else
            {
                var below = await _context.ProgramIntakes
                    .Where(x => x.DisplayOrder == p.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; p.DisplayOrder++; }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}