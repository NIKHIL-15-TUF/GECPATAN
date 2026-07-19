using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    // Handles menu items whose target is a dynamic Content Page.
    // Clean URL: /page/{slug}
    public class PageController : Controller
    {
        private readonly IContentPageApiService _pages;

        public PageController(IContentPageApiService pages)
        {
            _pages = pages;
        }

        [HttpGet("/page/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var page = await _pages.GetBySlugAsync(slug);
            if (page == null) return NotFound(); // ASSUMPTION: swap for your shared 404/ErrorViewModel handling if you have one

            ViewBag.Title = page.Title;
            return View(page);
        }
    }
}
