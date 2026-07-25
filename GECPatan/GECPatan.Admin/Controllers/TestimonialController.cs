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

        public TestimonialController(ApplicationDbContext context)
        {
            _context = context;
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

            _context.Testimonials.Add(new Testimonial
            {
                StudentName = model.StudentName,
                Department = model.Department,
                PassoutYear = model.PassoutYear,
                TestimonialText = model.TestimonialText,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Testimonial added.";
            return RedirectToAction(nameof(Index));
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

            await _context.SaveChangesAsync();
            TempData["Success"] = "Testimonial updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            t.IsVisible = !t.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            t.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Testimonial deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}