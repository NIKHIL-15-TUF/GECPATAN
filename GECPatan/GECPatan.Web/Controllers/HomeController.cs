using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GECPatan.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMenuApiService _menuApi;
        private readonly IHomeApiService _homeApi;
        private readonly IDepartmentApiService _departmentApi;
        private readonly IConfiguration _configuration;

        public HomeController(
            IMenuApiService menuApi,
            IHomeApiService homeApi,
            IDepartmentApiService departmentApi,
            IConfiguration configuration)
        {
            _menuApi = menuApi;
            _homeApi = homeApi;
            _departmentApi = departmentApi;
            _configuration = configuration;
        }

        // GET /
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var slidersTask = _homeApi.GetSlidersAsync(ct);
            var marqueeTask = _homeApi.GetMarqueeAsync(ct);
            var updatesTask = _homeApi.GetUpdatesAsync(20, ct);
            var testimonialsTask = _homeApi.GetTestimonialsAsync(ct);
            var topRecruitersTask = _homeApi.GetTopRecruitersAsync(ct);
            var activitiesTask = _homeApi.GetLatestActivitiesAsync(10, ct);
            var statsTask = _homeApi.GetStatsAsync(ct);
            var settingsTask = _homeApi.GetSettingsAsync(ct);
            var principalTask = _homeApi.GetPrincipalMessageAsync(ct);
            var departmentsTask = _departmentApi.GetAllDepartmentsAsync(ct);

            await Task.WhenAll(
                slidersTask, marqueeTask, updatesTask, testimonialsTask,
                topRecruitersTask, activitiesTask, statsTask, settingsTask,
                principalTask, departmentsTask);

            var vm = new HomeViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Sliders = slidersTask.Result,
                Marquee = marqueeTask.Result,
                Updates = updatesTask.Result,
                Testimonials = testimonialsTask.Result,
                TopRecruiters = topRecruitersTask.Result,
                Activities = activitiesTask.Result,
                Departments = departmentsTask.Result
                    .OrderBy(d => d.DisplayOrder)
                    .ToList(),
                Stats = statsTask.Result,
                Settings = settingsTask.Result,
                Principal = principalTask.Result
            };

            return View(vm);
        }

        // GET /Home/Menu — loaded via AJAX by _Layout.cshtml into <header id="menuload">
        [HttpGet]
        public async Task<IActionResult> Menu(CancellationToken ct)
        {
            var menu = await _menuApi.GetMainMenuAsync(ct);
            return PartialView("~/Views/Shared/_Menu.cshtml", menu);
        }

        // GET /Home/TopMenu — loaded via AJAX by _Layout.cshtml into <div id="topHeader">
        [HttpGet]
        public async Task<IActionResult> Header(CancellationToken ct)
        {
            var topMenuTask = _menuApi.GetTopMenuAsync(ct);
            var settingsTask = _homeApi.GetSettingsAsync(ct);

            await Task.WhenAll(topMenuTask, settingsTask);

            var settings = settingsTask.Result;

            var vm = new HeaderViewModel
            {
                TopMenu = topMenuTask.Result,
                ContactNo = settings?.Phone,
                ContactEmail = settings?.Email
            };

            return PartialView("~/Views/Shared/_Header.cshtml", vm);
        }

        // GET /Home/Footer — loaded via AJAX by _Layout.cshtml into <div id="Gecfooter">
        [HttpGet]
        public async Task<IActionResult> Footer(CancellationToken ct)
        {
            var footerMenuTask = _menuApi.GetFooterMenuAsync(ct);
            var settingsTask = _homeApi.GetSettingsAsync(ct);

            await Task.WhenAll(footerMenuTask, settingsTask);

            var settings = settingsTask.Result;

            var vm = new FooterViewModel
            {
                FooterMenu = footerMenuTask.Result,
                ContactNo = settings?.Phone,
                ContactEmail = settings?.Email,
                Address = settings?.Address,
                MapEmbedUrl = settings?.MapEmbedUrl,
                FacebookUrl = settings?.FacebookUrl,
                TwitterUrl = settings?.TwitterUrl,
                LinkedInUrl = settings?.LinkedInUrl,
                InstagramUrl = settings?.InstagramUrl
            };

            return PartialView("~/Views/Shared/_Footer.cshtml", vm);
        }

        // GET /Home/Error — registered as the global exception handler path in Program.cs
        [Route("Home/Error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
