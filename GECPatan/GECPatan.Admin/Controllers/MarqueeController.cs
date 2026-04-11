using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class MarqueeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarqueeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Marquee Items";
            var items = await _context.Marquees
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Marquee Item";
            return View(new MarqueeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarqueeVM model)
        {
            ViewData["Title"] = "Add Marquee Item";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.Marquees.Select(m => (int?)m.DisplayOrder).MaxAsync() ?? -1;

            _context.Marquees.Add(new Marquee
            {
                Title = model.Title,
                MarqueeType = model.MarqueeType,
                FileLink = model.FileLink,
                ControllerName = model.ControllerName,
                ActionName = model.ActionName,
                DynamicId = model.DynamicId,
                IsFile = model.IsFile,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Marquee item added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Marquee Item";
            var m = await _context.Marquees.FindAsync(id);
            if (m == null) return NotFound();

            return View(new MarqueeVM
            {
                Id = m.Id,
                Title = m.Title,
                MarqueeType = m.MarqueeType,
                FileLink = m.FileLink,
                ControllerName = m.ControllerName,
                ActionName = m.ActionName,
                DynamicId = m.DynamicId,
                IsFile = m.IsFile,
                DisplayOrder = m.DisplayOrder,
                IsVisible = m.IsVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarqueeVM model)
        {
            ViewData["Title"] = "Edit Marquee Item";
            if (!ModelState.IsValid) return View(model);

            var m = await _context.Marquees.FindAsync(id);
            if (m == null) return NotFound();

            m.Title = model.Title;
            m.MarqueeType = model.MarqueeType;
            m.FileLink = model.FileLink;
            m.ControllerName = model.ControllerName;
            m.ActionName = model.ActionName;
            m.DynamicId = model.DynamicId;
            m.IsFile = model.IsFile;
            m.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Marquee item updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var m = await _context.Marquees.FindAsync(id);
            if (m == null) return NotFound();
            m.IsVisible = !m.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.Marquees.FindAsync(id);
            if (m == null) return NotFound();
            m.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marquee item deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}