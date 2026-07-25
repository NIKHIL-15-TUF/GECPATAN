 using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;

namespace GECPatan.Core.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principal")]
    public class AboutUsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        public AboutUsController(ApplicationDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
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
            //await _notify.SendAsync(
            //    title: $"About Us : {model.HistoryText}",
            //    message: $"About Us Page has been updated",
            //    module: "AboutUs",
            //    icon: "fa-user-plus",
            //    color: "success",
            //    link: $"/AboutUs/Index",
            //    forRole: "SuperAdmin"
            //);
            return RedirectToAction(nameof(Index));
        }
    }
}
