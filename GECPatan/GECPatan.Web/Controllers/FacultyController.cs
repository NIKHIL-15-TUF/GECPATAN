using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class FacultyController : Controller
    {
        private readonly IFacultyApiService _api;
        private readonly IConfiguration _configuration;

        public FacultyController(IFacultyApiService api, IConfiguration configuration)
        {
            _api = api;
            _configuration = configuration;
        }

        // GET /Faculty/Details/202
        // Returns a layout-less partial, loaded into the "KNOW MORE" popup
        // via $("#pdfreader").load(url) -- see loadFacultyinfo() in
        // Department/Index.cshtml.
        [HttpGet("Faculty/Details/{id:int}")]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var faculty = await _api.GetFacultyDetailAsync(id, ct);
            if (faculty is null)
            {
                return NotFound();
            }

            var vm = new FacultyDetailsViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                FacultyId = faculty.FacultyId,
                Name = faculty.Name,
                Designation = faculty.Designation,
                ImagePath = faculty.ImagePath,
                AreaOfInterest = faculty.AreaOfInterest,
                Website = faculty.Website,
                IsTeaching = faculty.IsTeaching,
                DateOfJoining = faculty.DateOfJoining,
                DeptName = faculty.DeptName,
                DeptShortCode = faculty.DeptShortCode,
                EducationalQualifications = faculty.Qualifications,
                ProfessionalExperiences = faculty.Experiences,
                TrainingAndWorkshops = faculty.Trainings,
                Publications = faculty.Publications
            };

            return View(vm);
        }
    }
}