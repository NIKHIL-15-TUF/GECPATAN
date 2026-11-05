using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class ActivityController : Controller
    {
        private readonly IActivityApiService _activityApi;
        private readonly IConfiguration _configuration;

        public ActivityController(
            IActivityApiService activityApi,
            IConfiguration configuration)
        {
            _activityApi = activityApi;
            _configuration = configuration;
        }

        // GET /Activities
        [HttpGet("/Activities")]
        public async Task<IActionResult> Index(
            int? year = null,
            int page = 1)
        {
            const int pageSize = 20;

            if (page < 1)
                page = 1;

            var result = await _activityApi.GetActivitiesAsync(
                year,
                page,
                pageSize);

            var model = new ActivityListViewModel
            {
                Activities = result.Data,
                Total = result.Total,
                Page = page,
                PageSize = pageSize,
                SelectedYear = year,
                Years = await _activityApi.GetYearsAsync(),
                ApiBaseUrl = _configuration["Api:BaseUrl"] ?? ""
            };

            return View(model);
        }

        // GET /activity/{id}
        [HttpGet("/activity/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var activity = await _activityApi.GetActivityByIdAsync(id);

            if (activity == null)
                return NotFound();

            var model = new ActivityDetailViewModel
            {
                Activity = activity,
                ApiBaseUrl = _configuration["Api:BaseUrl"] ?? ""
            };

            return View(model);
        }
    }
}