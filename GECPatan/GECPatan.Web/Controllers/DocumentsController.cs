using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly IDocumentApiService _documents;
        private readonly IConfiguration _config;

        public DocumentsController(IDocumentApiService documents, IConfiguration config)
        {
            _documents = documents;
            _config = config;
        }

        // GET /Documents
        // Lists all visible document categories (Mandatory Disclosure, Central Store
        // & Purchase, Academic Calendar, or any other category the Admin creates).
        // MoU / SSIP / Timetable / Tenders keep their own dedicated pages and are not
        // listed here.
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var categoriesTask = _documents.GetCategoriesAsync(ct);
            var mouTask = _documents.GetMoUAsync(ct);
            var ssipTask = _documents.GetSSIPAsync(ct);
            var timetableTask = _documents.GetTimetablesAsync(ct);

            await Task.WhenAll(categoriesTask, mouTask, ssipTask, timetableTask);

            var vm = new DocumentCategoryListViewModel
            {
                Categories = categoriesTask.Result,
                MoUCount = mouTask.Result.Count,
                SSIPCount = ssipTask.Result.Count,
                TimetableCount = timetableTask.Result.Count,
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = "Documents";
            return View(vm);
        }

        // GET /Documents/TableView/5
        // Flat, numbered table listing for a category — an alternate layout to
        // the card/grid Category view, for categories better suited to a plain
        // list (e.g. Academic Calendar, Mandatory Disclosure).
        public async Task<IActionResult> TableView(int id, CancellationToken ct)
        {
            var category = await _documents.GetCategoryDetailAsync(id, ct);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new DocumentCategoryDetailViewModel
            {
                Category = category,
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = category.Title;
            return View(vm);
        }

        // GET /Documents/Category/5
        // Year-wise file listing for a single category.
        public async Task<IActionResult> Category(int id, CancellationToken ct)
        {
            var category = await _documents.GetCategoryDetailAsync(id, ct);

            if (category == null)
            {
                return NotFound();
            }

            var vm = new DocumentCategoryDetailViewModel
            {
                Category = category,
                ApiBaseUrl = _config["Api:BaseUrl"]
            };

            ViewBag.Title = category.Title;
            return View(vm);
        }

        public async Task<IActionResult> MoU(CancellationToken ct)
        {
            var data = await _documents.GetMoUAsync(ct);
            var vm = new SimpleDocumentListViewModel
            {
                PageTitle = "Memorandum of Understanding (MoU)",
                SubLabelHeader = "Month/Year",
                Rows = data.OrderBy(d => d.DisplayOrder)
                    .Select(d => new SimpleDocumentRow { Title = d.Title, SubLabel = d.MonthYear ?? "", FilePath = d.FilePath })
                    .ToList(),
                ApiBaseUrl = _config["Api:BaseUrl"]
            };
            ViewBag.Title = vm.PageTitle;
            return View("SimpleList", vm);
        }

        public async Task<IActionResult> SSIP(CancellationToken ct)
        {
            var data = await _documents.GetSSIPAsync(ct);
            var vm = new SimpleDocumentListViewModel
            {
                PageTitle = "Student Startup & Innovation Policy (SSIP)",
                SubLabelHeader = "Upload Date",
                Rows = data.OrderBy(d => d.DisplayOrder)
                    .Select(d => new SimpleDocumentRow { Title = d.Title, SubLabel = d.UploadDate ?? "", FilePath = d.FilePath })
                    .ToList(),
                ApiBaseUrl = _config["Api:BaseUrl"]
            };
            ViewBag.Title = vm.PageTitle;
            return View("SimpleList", vm);
        }

        public async Task<IActionResult> Timetable(CancellationToken ct)
        {
            var data = await _documents.GetTimetablesAsync(ct);
            var vm = new SimpleDocumentListViewModel
            {
                PageTitle = "Time Table",
                SubLabelHeader = "Semester",
                Rows = data
                    .Select(t => new SimpleDocumentRow
                    {
                        Title = t.DeptName,
                        SubLabel = $"Sem {t.Semester} ({t.SemesterType}), {t.Year}",
                        FilePath = t.FilePath
                    }).ToList(),
                ApiBaseUrl = _config["Api:BaseUrl"]
            };
            ViewBag.Title = vm.PageTitle;
            return View("SimpleList", vm);
        }
    }
}
