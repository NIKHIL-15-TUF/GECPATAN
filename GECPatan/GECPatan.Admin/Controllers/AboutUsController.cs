using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principal")]
    public class AboutUsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutUsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "About Us";
            var about = await _context.AboutUs.FirstOrDefaultAsync();
            if (about == null)
                return View(new AboutUsVM());

            return View(new AboutUsVM
            {
                Id = about.Id,
                HistoryText = about.HistoryText
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AboutUsVM model)
        {
            ViewData["Title"] = "About Us";
            if (!ModelState.IsValid) return View(model);

            var about = await _context.AboutUs.FirstOrDefaultAsync();
            if (about == null)
            {
                _context.AboutUs.Add(new AboutUs { HistoryText = model.HistoryText });
            }
            else
            {
                about.HistoryText = model.HistoryText;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "About Us updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
