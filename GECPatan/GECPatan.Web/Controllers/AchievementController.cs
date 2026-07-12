using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class AchievementController : Controller
    {
        private readonly IAchievementApiService _api;
        private readonly IConfiguration _configuration;

        public AchievementController(IAchievementApiService api, IConfiguration configuration)
        {
            _api = api;
            _configuration = configuration;
        }

        // GET /Achievements
        // Renders the year tab bar, then kicks off an initial AJAX load of
        // ByYear(0) ("All") client-side -- see Views/Achievement/Index.cshtml.
        [HttpGet("Achievements")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var all = await _api.GetAllAsync(ct: ct);

            var vm = new AchievementsViewModel
            {
                Years = all
                    .Where(a => a.Year.HasValue)
                    .Select(a => a.Year!.Value)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList()
            };

            return View(vm);
        }

        // GET /Achievements/ByYear?year=2025   (year = 0 means "All")
        // Returns a layout-less partial that's AJAX-loaded into #achDiv and
        // then re-initialised as a CubePortfolio grid -- see loadAchievements()
        // in Views/Achievement/Index.cshtml.
        //
        // NOTE: GET /api/achievements has no year filter, so we fetch once and
        // filter by year here rather than adding an unsupported query param.
        [HttpGet("Achievements/ByYear")]
        public async Task<IActionResult> ByYear(int year, CancellationToken ct)
        {
            var all = await _api.GetAllAsync(ct: ct);

            var filtered = year == 0
                ? all
                : all.Where(a => a.Year == year).ToList();

            var vm = new AchievementListViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Achievements = filtered
                    .OrderByDescending(a => a.Year)
                    .ThenByDescending(a => a.Date)
                    .ToList(),
                Types = filtered
                    .Where(a => !string.IsNullOrWhiteSpace(a.TypeName))
                    .Select(a => a.TypeName!)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList()
            };

            return PartialView("_AchievementList", vm);
        }
    }
}
