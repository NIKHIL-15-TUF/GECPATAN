using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class GalleryController : Controller
    {
        private readonly IGalleryApiService _gallery;
        private readonly IConfiguration _config;

        public GalleryController(IGalleryApiService gallery, IConfiguration config)
        {
            _gallery = gallery;
            _config = config;
        }

        // GET /Gallery
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var images = await _gallery.GetAllAsync(ct);

            var vm = new GalleryViewModel
            {
                Images = images.OrderBy(i => i.DisplayOrder).ToList(),
                Categories = images
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList(),
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = "Gallery";
            return View(vm);
        }
    }
}
