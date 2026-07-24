using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class TendersController : Controller
    {
        private readonly ITenderApiService _tenders;
        private readonly IConfiguration _config;

        public TendersController(ITenderApiService tenders, IConfiguration config)
        {
            _tenders = tenders;
            _config = config;
        }

        // GET /Tenders
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var groups = await _tenders.GetAllAsync(ct);

            // Only publicly-relevant documents; drop empty groups after filtering.
            foreach (var g in groups)
            {
                g.Documents = g.Documents
                    .Where(d => d.IsActive && !d.IsExpired)
                    .ToList();
            }
            groups = groups
                .Where(g => g.Documents.Any())
                .OrderBy(g => g.DisplayOrder)
                .ToList();

            var vm = new TenderViewModel
            {
                Groups = groups,
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = "Tenders";
            return View(vm);
        }
    }
}
