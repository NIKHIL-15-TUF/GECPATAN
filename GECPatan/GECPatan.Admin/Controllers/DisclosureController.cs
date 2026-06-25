using GECPatan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class DisclosureController : Controller
    {
        private readonly DisclosureDataService _dataService;

        public DisclosureController(DisclosureDataService dataService)
            => _dataService = dataService;

        // GET /Disclosure/Preview?year=2025-26
        // Full HTML preview of the Mandatory Disclosure
        // document. Also used as the PDF render source.
        public async Task<IActionResult> Preview(string? year)
        {
            string academicYear = string.IsNullOrWhiteSpace(year)
                ? $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1) % 100:D2}"
                : year;

            ViewData["Title"] = $"Mandatory Disclosure Preview — {academicYear}";

            var data = await _dataService.BuildAsync(academicYear);

            // Use a bare layout for preview (no admin sidebar)
            // so it looks like the final document
            return View("Preview", data);
        }
    }
}