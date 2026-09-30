using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class FacilitiesController : Controller
    {
        private readonly IFacilityApiService _facilities;
        private readonly IConfiguration _config;

        public FacilitiesController(IFacilityApiService facilities, IConfiguration config)
        {
            _facilities = facilities;
            _config = config;
        }

        // GET /Facilities
        //public async Task<IActionResult> Index(CancellationToken ct)
        //{
        //    var facilities = await _facilities.GetAllFacilitiesAsync(ct);

        //    var vm = new FacilityListViewModel
        //    {
        //        Facilities = facilities,
        //        ApiBaseUrl = _config["Api:BaseUrl"]
        //    };

        //    ViewBag.Title = "Facilities";
        //    return View(vm);
        //}

        // GET /Facilities/Details/{id}
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var f = await _facilities.GetFacilityDetailAsync(id, ct);
            if (f == null) return NotFound();

            var vm = new FacilityViewModel
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                BlogspotLink = f.BlogspotLink,
                About = f.About,
                TitleImagePath = f.TitleImagePath,
                BannerImages = f.BannerImages,
                Visions = f.Visions,
                Missions = f.Missions,
                Members = f.Members,
                DynamicSections = f.DynamicSections,
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = f.Title;
            return View(vm);
        }
    }
}
