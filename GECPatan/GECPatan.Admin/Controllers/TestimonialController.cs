using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class TestimonialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestimonialController> _logger;

        public TestimonialController(ApplicationDbContext context, ILogger<TestimonialController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Testimonials";
            var items = await _context.Testimonials
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Testimonial";
            return View(new TestimonialVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestimonialVM model)
        {
            ViewData["Title"] = "Add Testimonial";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.Testimonials.Select(t => (int?)t.DisplayOrder).MaxAsync() ?? -1;

            var testimonial = new Testimonial
            {
                StudentName = model.StudentName,
                Department = model.Department,
                PassoutYear = model.PassoutYear,
                TestimonialText = model.TestimonialText,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.Testimonials.Add(testimonial);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Testimonial {TestimonialId} created for '{StudentName}'",
                    testimonial.Id, testimonial.StudentName);

                TempData["Success"] = "Testimonial added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating testimonial for '{StudentName}'", model.StudentName);
                ModelState.AddModelError(string.Empty, "Unable to save the testimonial. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating testimonial for '{StudentName}'", model.StudentName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Testimonial";
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();

            return View(new TestimonialVM
            {
                Id = t.Id,
                StudentName = t.StudentName,
                Department = t.Department,
                PassoutYear = t.PassoutYear,
                TestimonialText = t.TestimonialText,
                DisplayOrder = t.DisplayOrder,
                IsVisible = t.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TestimonialVM model)
        {
            ViewData["Title"] = "Edit Testimonial";
            if (!ModelState.IsValid) return View(model);

            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();

            t.StudentName = model.StudentName;
            t.Department = model.Department;
            t.PassoutYear = model.PassoutYear;
            t.TestimonialText = model.TestimonialText;
            t.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Testimonial {TestimonialId} updated", t.Id);

                TempData["Success"] = "Testimonial updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating testimonial {TestimonialId}", id);
                ModelState.AddModelError(string.Empty,
                    "This testimonial was changed by someone else. Please reload and try again.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating testimonial {TestimonialId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the testimonial. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating testimonial {TestimonialId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();

            t.IsVisible = !t.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Testimonial {TestimonialId} visibility set to {IsVisible}", id, t.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for testimonial {TestimonialId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();

            t.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Testimonial {TestimonialId} deleted", id);
                TempData["Success"] = "Testimonial deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting testimonial {TestimonialId}", id);
                TempData["Error"] = "Unable to delete the testimonial. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}