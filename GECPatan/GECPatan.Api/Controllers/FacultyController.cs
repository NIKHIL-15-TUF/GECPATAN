using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/faculty")]
    public class FacultyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FacultyController(ApplicationDbContext context)
            => _context = context;

        // GET /api/faculty
        // All active faculty (across departments)
        // Optional: ?deptId=1 to filter by department
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FacultyListDTO>>>> GetAll(
            [FromQuery] int? deptId = null)
        {
            var query = _context.Faculties
                .Include(f => f.Department)
                .Where(f => f.IsActive);

            if (deptId.HasValue)
                query = query.Where(f => f.DeptId == deptId.Value);

            var data = await query
                .OrderBy(f => f.DeptId)
                .ThenBy(f => f.SeniorityOrder)
                .ThenBy(f => f.Name)
                .Select(f => new FacultyListDTO
                {
                    FacultyId = f.FacultyId,
                    Name = f.Name,
                    Designation = f.Designation,
                    ImagePath = f.ImagePath,
                    AreaOfInterest = f.AreaOfInterest,
                    IsTeaching = f.IsTeaching,
                    SeniorityOrder = f.SeniorityOrder,
                    DeptId = f.DeptId,
                    DeptName = f.Department != null
                        ? f.Department.Name : "",
                    DeptShortCode = f.Department != null
                        ? f.Department.ShortCode : null
                })
                .ToListAsync();

            return Ok(ApiResponse<List<FacultyListDTO>>.Ok(data));
        }

        // GET /api/faculty/{id}
        // Full public profile
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FacultyDetailDTO>>> GetById(int id)
        {
            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .Include(f => f.Qualifications)
                .Include(f => f.Experiences)
                .Include(f => f.Trainings)
                .Include(f => f.Publications)
                .FirstOrDefaultAsync(f => f.FacultyId == id && f.IsActive);

            if (faculty == null)
                return NotFound(ApiResponse<FacultyDetailDTO>.Fail(
                    "Faculty not found"));

            var data = new FacultyDetailDTO
            {
                FacultyId = faculty.FacultyId,
                Name = faculty.Name,
                Designation = faculty.Designation,
                ImagePath = faculty.ImagePath,
                AreaOfInterest = faculty.AreaOfInterest,
                Website = faculty.Website,
                IsTeaching = faculty.IsTeaching,
                DateOfJoining = faculty.DateOfJoining.ToString("yyyy-MM-dd"),

                DeptId = faculty.DeptId,
                DeptName = faculty.Department?.Name ?? "",
                DeptShortCode = faculty.Department?.ShortCode,

                Qualifications = faculty.Qualifications
                    .Select(q => new FacultyQualificationDTO
                    {
                        Degree = q.Degree,
                        University = q.University,
                        Year = q.Year,
                        Specialization = q.Specialization
                    }).ToList(),

                Experiences = faculty.Experiences
                    .Select(e => new FacultyExperienceDTO
                    {
                        Position = e.Position,
                        Organization = e.Organization,
                        FromDate = e.FromDate.HasValue
                            ? e.FromDate.Value.ToString("yyyy-MM-dd") : null,
                        ToDate = e.ToDate.HasValue
                            ? e.ToDate.Value.ToString("yyyy-MM-dd") : null
                    }).ToList(),

                Trainings = faculty.Trainings
                    .Select(t => new FacultyTrainingDTO
                    {
                        Title = t.Title,
                        OrganizedBy = t.OrganizedBy,
                        FromDate = t.FromDate.HasValue
                            ? t.FromDate.Value.ToString("yyyy-MM-dd") : null,
                        ToDate = t.ToDate.HasValue
                            ? t.ToDate.Value.ToString("yyyy-MM-dd") : null
                    }).ToList(),

                Publications = faculty.Publications
                    .OrderBy(p => p.SrNo)
                    .Select(p => new FacultyPublicationDTO
                    {
                        SrNo = p.SrNo,
                        Title = p.Title
                    }).ToList()
            };

            return Ok(ApiResponse<FacultyDetailDTO>.Ok(data));
        }
    }
}