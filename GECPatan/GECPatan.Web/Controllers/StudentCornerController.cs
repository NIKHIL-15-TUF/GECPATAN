using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    // Old version of this controller read ~/Data/Facilities/StudentClub.json and
    // ~/Data/Activities/Activities.json straight off disk in the constructor
    // (via WebClient.DownloadString on a local file path) and kept them in memory
    // for the lifetime of the controller instance. That meant every edit made in
    // the Admin app needed an app restart to show up on the public site, and the
    // controller couldn't be load-balanced across multiple servers because each
    // instance would read its own local copy of the files.
    //
    // This version instead calls GECPatan.Api's /api/clubs endpoints through
    // IStudentClubApiService (HttpClient-based, same pattern as
    // IDepartmentApiService / IFacultyApiService), so content edited in the Admin
    // app shows up immediately and the Web app never touches the database or the
    // file system directly.
    public class StudentCornerController : Controller
    {
        private readonly IStudentClubApiService _clubApi;
        private readonly IConfiguration _configuration;

        public StudentCornerController(IStudentClubApiService clubApi, IConfiguration configuration)
        {
            _clubApi = clubApi;
            _configuration = configuration;
        }

        // GET /StudentCorner/StudentClubs
        // Replaces: return View(ClubsVMs); where ClubsVMs came from StudentClub.json.
        [HttpGet("StudentCorner/StudentClubs")]
        public async Task<IActionResult> StudentClubs(CancellationToken ct)
        {
            var clubs = await _clubApi.GetAllClubsAsync(ct);

            var vm = new StudentClubsViewModel
            {
                // Cover images/icons are served by GECPatan.Api's own static file
                // middleware, not by this Web app -- see ResolveApiFileUrl() in
                // the view, same reasoning as DepartmentViewModel.ApiBaseUrl.
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Title = "Student Clubs",
                Clubs = clubs
            };

            return View(vm);
        }

        // GET /StudentCorner/ClubDetails/5
        // Replaces: ClubsVMs.Clubs.FirstOrDefault(m => m.ID == id) +
        // ActivitiesVM.Where(m => m.ClubId == id) from JSON files.
        //
        // Also answers GET /clubs/5: GECPatan.Api's ClubsController.ResolveClubLink()
        // returns "/clubs/{id}" as the public URL for non-dynamic clubs (see
        // StudentClubListDTO.Link / StudentClubDetailDTO.Link), so the views link
        // straight to that path. Without this second route, following that link
        // 404s because nothing in GECPatan.Web was mapped to "/clubs/{id}".
        [HttpGet("StudentCorner/ClubDetails/{id:int}")]
        [HttpGet("clubs/{id:int}")]
        public async Task<IActionResult> ClubDetails(int id, CancellationToken ct)
        {
            var club = await _clubApi.GetClubDetailAsync(id, ct);
            if (club is null)
            {
                return NotFound();
            }

            // GET /api/clubs/{id}/activities fetched separately in parallel,
            // same shape as DepartmentController firing its detail calls together.
            var activities = await _clubApi.GetActivitiesAsync(id, ct);

            var vm = new ClubDetailsViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Id = club.Id,
                Title = club.Title,
                About = club.About,
                BlogLink = club.BlogLink,
                CoverImage = club.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath,
                Images = club.Images,
                Members = club.Members,
                SubObjectives = club.Objectives,
                DynamicSections = club.DynamicSections,
                Activities = activities
            };

            return View(vm);
        }

        // ── NOT YET MIGRATED ────────────────────────────────
        // These actions used MOUModelVM, populated from ~/MOU/MPU.json in the old
        // constructor. GECPatan.Api already exposes the equivalent data at
        // GET /api/documents/mou (see MoUDocumentDTO), but there is no
        // IMoUApiService in GECPatan.Web yet. Wire these up the same way as
        // IStudentClubApiService above once that service exists -- out of scope
        // for this club-pages migration.
        public IActionResult Rules() => View();
        public IActionResult TimeTable() => View();
        public IActionResult Enroll() => View();
        public IActionResult StudentGradeHistory() => View();
        public IActionResult Fees() => View();
    }
}