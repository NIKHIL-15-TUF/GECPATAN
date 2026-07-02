using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentApiService _api;

        public DepartmentController(IDepartmentApiService api)
        {
            _api = api;
        }

        // GET /Department/Index/5
        [HttpGet("Department/{id:int}")]
        public async Task<IActionResult> Index(int id, CancellationToken ct)
        {
            var detailTask = _api.GetDepartmentDetailAsync(id, ct);
            var facultyTask = _api.GetFacultyAsync(id, ct);
            var labsTask = _api.GetLabsAsync(id, ct);
            var timetableTask = _api.GetTimetableAsync(id, ct);
            var noticesTask = _api.GetNoticesAsync(id, ct);
            var activitiesTask = _api.GetActivitiesAsync(id, ct);

            await Task.WhenAll(detailTask, facultyTask, labsTask, timetableTask, noticesTask, activitiesTask);

            var detail = await detailTask;
            if (detail == null)
                return NotFound();

            var vm = new DepartmentViewModel
            {
                DeptId = detail.DeptId,
                Title = detail.Name,
                About = detail.About,
                TitleImagePath = detail.TitleImagePath,
                Tagline = detail.Tagline,
                ShowIntake = detail.ShowIntake,
                Intake = detail.CurrentIntake,
                AnnualPlacementCount = detail.AnnualPlacement,
                FacultyCount = detail.FacultyCount,
                LabCount = detail.LabCount,
                BannerImages = detail.BannerImages,
                Vision = detail.Visions,
                Mission = detail.Missions,
                PEOs = detail.PEOs,
                PSOs = detail.PSOs,
                DynamicSections = detail.DynamicSections,
                FacultyList = await facultyTask,
                Labs = await labsTask,
                TimeTable = await timetableTask,
                NoticeBoard = await noticesTask,
                Activities = await activitiesTask
            };

            return View(vm);
        }
    }
}
