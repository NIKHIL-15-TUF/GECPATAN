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

        public HomeController(IMenuApiService menuApi, IHomeApiService homeApi)
        {
            _menuApi = menuApi;
            _homeApi = homeApi;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET /Home/Menu — loaded via AJAX by _Layout.cshtml into <header id="menuload">
        [HttpGet]
        public async Task<IActionResult> Menu(CancellationToken ct)
        {
            var menu = await _menuApi.GetMainMenuAsync(ct);
            return PartialView("~/Views/Shared/_Header.cshtml", menu);
        }

        // GET /Home/TopMenu — loaded via AJAX by _Layout.cshtml into <div id="topHeader">
        [HttpGet]
        public async Task<IActionResult> TopMenu(CancellationToken ct)
        {
            var topMenuTask = _menuApi.GetTopMenuAsync(ct);
            var settingsTask = _homeApi.GetSettingsAsync(ct);

            await Task.WhenAll(topMenuTask, settingsTask);

            var settings = settingsTask.Result;

            var vm = new TopHeaderViewModel
            {
                TopMenu = topMenuTask.Result,
                ContactNo = settings?.Phone,
                ContactEmail = settings?.Email
            };

            return PartialView("~/Views/Shared/_TopHeader.cshtml", vm);
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
