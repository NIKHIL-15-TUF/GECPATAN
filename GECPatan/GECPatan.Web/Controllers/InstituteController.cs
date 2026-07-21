using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    // NOTE: only the NewsLetter action is implemented here. The seeded menu
    // also expects Institute/AboutUs -- add that action here too, following
    // the same pattern, when its API endpoint exists.
    public class InstituteController : Controller
    {
        private readonly INewsApiService _newsApi;
        private readonly IConfiguration _configuration;

        public InstituteController(INewsApiService newsApi, IConfiguration configuration)
        {
            _newsApi = newsApi;
            _configuration = configuration;
        }

        // GET /Institute/NewsLetter
        // Matches the seeded menu item: M("Newsletter", inst.Id, "internal",
        // "Institute", "NewsLetter", pos: 4) -- keep controller/action names
        // exactly as they are or the "Newsletter" nav link will 404.
        [HttpGet("Institute/NewsLetter")]
        public async Task<IActionResult> NewsLetter(CancellationToken ct)
        {
            var letters = await _newsApi.GetLettersAsync(ct);

            var vm = new NewsLetterViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Letters = letters.OrderBy(l => l.DisplayOrder).ToList()
            };

            return View(vm);
        }
    }
}
