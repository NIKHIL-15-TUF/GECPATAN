using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentApiService _api;
        private readonly IConfiguration _configuration;

        public DepartmentController(IDepartmentApiService api, IConfiguration configuration)
        {
            _api = api;
            _configuration = configuration;
        }

        // GET /Department/5
        [HttpGet("Department/{id:int}")]
        public async Task<IActionResult> Index(int id, CancellationToken ct)
        {
            var dept = await _api.GetDepartmentDetailAsync(id, ct);
            if (dept is null)
            {
                return NotFound();
            }

            // Fire all the detail calls in parallel instead of one-at-a-time.
            var facultyTask = _api.GetFacultyAsync(id, ct);
            var labsTask = _api.GetLabsAsync(id, ct);
            var timetableTask = _api.GetTimetableAsync(id, ct);
            var intakeTask = _api.GetIntakeAsync(id, ct);
            var noticesTask = _api.GetNoticesAsync(id, ct);
            var activitiesTask = _api.GetActivitiesAsync(id, ct);

            await Task.WhenAll(facultyTask, labsTask, timetableTask, intakeTask, noticesTask, activitiesTask);

            var vm = new DepartmentViewModel
            {
                // Uploaded files (PDFs, faculty/lab/banner images) are served by
                // GECPatan.Api's own static file middleware, not by this Web app.
                // Relative paths like "/uploads/timetables/xxx.pdf" returned by
                // the API therefore need to be resolved against the API's own
                // host, not this app's — see ResolveApiFileUrl() in the view.
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                DeptId = dept.DeptId,
                Title = dept.Name,
                About = dept.About,
                TitleImagePath = dept.TitleImagePath,
                Tagline = dept.Tagline,
                ShowIntake = dept.ShowIntake,
                Intake = dept.CurrentIntake,
                AnnualPlacementCount = dept.AnnualPlacement,
                FacultyCount = dept.FacultyCount,
                LabCount = dept.LabCount,
                BannerImages = dept.BannerImages,
                Vision = dept.Visions,
                Mission = dept.Missions,
                PEOs = dept.PEOs,
                PSOs = dept.PSOs,
                DynamicSections = dept.DynamicSections,
                FacultyList = facultyTask.Result,
                Labs = labsTask.Result,
                TimeTable = timetableTask.Result,
                IntakeHistory = intakeTask.Result,
                NoticeBoard = noticesTask.Result,
                Activities = activitiesTask.Result
            };

            return View(vm);
        }

        // GET /Department  (optional department listing page, uses GET /api/departments)
        [HttpGet("Department")]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var departments = await _api.GetAllDepartmentsAsync(ct);
            return View(departments);
        }
    }
}