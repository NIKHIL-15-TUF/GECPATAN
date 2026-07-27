using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class ResearchGrantsController : Controller
    {
        private readonly IResearchGrantApiService _grants;

        public ResearchGrantsController(IResearchGrantApiService grants)
        {
            _grants = grants;
        }

        // GET /ResearchGrants
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var grants = await _grants.GetAllAsync(ct);
            var ordered = grants.OrderBy(g => g.DisplayOrder).ToList();

            ViewBag.Title = "Research Grants";
            return View(ordered);
        }
    }
}
