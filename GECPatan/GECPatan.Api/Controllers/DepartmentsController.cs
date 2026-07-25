using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
            => _context = context;

        // GET /api/departments
        // All active departments (list)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DepartmentListDTO>>>> GetAll()
        {
            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            var deptIds = depts.Select(d => d.DeptId).ToList();

            var facultyCounts = await _context.Faculties
                .Where(f => f.IsActive && deptIds.Contains(f.DeptId))
                .GroupBy(f => f.DeptId)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToListAsync();

            var labCounts = await _context.Labs
                .Where(l => deptIds.Contains(l.DeptId))
                .GroupBy(l => l.DeptId)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToListAsync();

            var latestIntakes = await _context.ProgramIntakes
                .Where(p => deptIds.Contains(p.DeptId))
                .GroupBy(p => p.DeptId)
                .Select(g => new
                {
                    DeptId = g.Key,
                    Intake = g.OrderByDescending(x => x.IntakeYear)
                        .Select(x => x.Intake)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var data = depts.Select(d => new DepartmentListDTO
            {
                DeptId = d.DeptId,
                Name = d.Name,
                ShortCode = d.ShortCode,
                Tagline = d.Tagline,
                TitleImagePath = d.TitleImagePath,
                DisplayOrder = d.DisplayOrder,
                FacultyCount = facultyCounts
                    .FirstOrDefault(f => f.DeptId == d.DeptId)?.Count ?? 0,
                LabCount = labCounts
                    .FirstOrDefault(l => l.DeptId == d.DeptId)?.Count ?? 0,
                CurrentIntake = latestIntakes
                    .FirstOrDefault(i => i.DeptId == d.DeptId)?.Intake ?? 0
            }).ToList();

            return Ok(ApiResponse<List<DepartmentListDTO>>.Ok(data));
        }

        // GET /api/departments/{id}
        // Department detail (includes Dynamic Sections)
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DepartmentDetailDTO>>> GetById(int id)
        {
            var dept = await _context.Departments
                .Include(d => d.BannerImages.OrderBy(b => b.DisplayOrder))
                .Include(d => d.Visions.OrderBy(v => v.DisplayOrder))
                .Include(d => d.Missions.OrderBy(m => m.DisplayOrder))
                .Include(d => d.PEOs.OrderBy(p => p.DisplayOrder))
                .Include(d => d.PSOs.OrderBy(p => p.DisplayOrder))
                .FirstOrDefaultAsync(d => d.DeptId == id && d.IsActive);

            if (dept == null)
                return NotFound(ApiResponse<DepartmentDetailDTO>.Fail(
                    "Department not found"));

            var facultyCount = await _context.Faculties
                .CountAsync(f => f.DeptId == id && f.IsActive);

            var labCount = await _context.Labs
                .CountAsync(l => l.DeptId == id);

            var latestIntake = await _context.ProgramIntakes
                .Where(p => p.DeptId == id)
                .OrderByDescending(p => p.IntakeYear)
                .FirstOrDefaultAsync();

            // ── Dynamic Sections for this department ──────
            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Department
                         && s.PageId == id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var data = new DepartmentDetailDTO
            {
                DeptId = dept.DeptId,
                Name = dept.Name,
                ShortCode = dept.ShortCode,
                About = dept.About,
                TitleImagePath = dept.TitleImagePath,
                Tagline = dept.Tagline,
                ShowIntake = dept.ShowIntake,
                AnnualPlacement = dept.AnnualPlacement,
                FacultyCount = facultyCount,
                LabCount = labCount,
                CurrentIntake = latestIntake?.Intake ?? 0,
                CurrentIntakeYear = latestIntake?.IntakeYear ?? 0,
                BannerImages = dept.BannerImages
                    .Select(b => b.ImagePath).ToList(),
                Visions = dept.Visions
                    .Select(v => v.VisionText).ToList(),
                Missions = dept.Missions
                    .Select(m => m.MissionText).ToList(),
                PEOs = dept.PEOs
                    .Select(p => p.PEOText).ToList(),
                PSOs = dept.PSOs
                    .Select(p => p.PSOText).ToList(),
                DynamicSections = dynamicSections.Select(s => new DynamicSectionDTO
                {
                    Id = s.Id,
                    Title = s.Title,
                    SectionType = s.SectionType.ToString(),
                    HtmlContent = s.HtmlContent,
                    FilePath = s.FilePath,
                    FileName = s.FileName,
                    DisplayOrder = s.DisplayOrder,
                    Files = s.Files
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => new DynamicSectionFileDTO
                        {
                            FilePath = f.FilePath,
                            Title = f.Title,
                            FileType = f.FileType,
                            DisplayOrder = f.DisplayOrder
                        }).ToList()
                }).ToList()
            };

            return Ok(ApiResponse<DepartmentDetailDTO>.Ok(data));
        }

        // GET /api/departments/{id}/faculty
        [HttpGet("{id}/faculty")]
        public async Task<ActionResult<ApiResponse<List<DeptFacultyDTO>>>> GetFaculty(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<DeptFacultyDTO>>.Fail(
                    "Department not found"));

            var data = await _context.Faculties
                .Where(f => f.DeptId == id && f.IsActive)
                .OrderBy(f => f.SeniorityOrder)
                .ThenBy(f => f.Name)
                .Select(f => new DeptFacultyDTO
                {
                    FacultyId = f.FacultyId,
                    Name = f.Name,
                    Designation = f.Designation,
                    ImagePath = f.ImagePath,
                    AreaOfInterest = f.AreaOfInterest,
                    IsTeaching = f.IsTeaching,
                    SeniorityOrder = f.SeniorityOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<DeptFacultyDTO>>.Ok(data));
        }
        // GET /api/departments/{id}/labs
        [HttpGet("{id}/labs")]
        public async Task<ActionResult<ApiResponse<List<DeptLabDTO>>>> GetLabs(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<DeptLabDTO>>.Fail(
                    "Department not found"));

            var labs = await _context.Labs
                .Include(l => l.Images.OrderBy(i => i.DisplayOrder))
                .Where(l => l.DeptId == id && l.IsVisible)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();

            var data = labs.Select(l => new DeptLabDTO
            {
                LabId = l.LabId,
                LabName = l.LabName,
                About = l.About,
                DisplayOrder = l.DisplayOrder,
                Images = l.Images.Select(img => new DeptLabImageDTO
                {
                    ImagePath = img.ImagePath,
                    Caption = img.Caption,
                    DisplayOrder = img.DisplayOrder
                }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<DeptLabDTO>>.Ok(data));
        }

        // GET /api/departments/{id}/timetable
        // Only latest timetables shown
        [HttpGet("{id}/timetable")]
        public async Task<ActionResult<ApiResponse<List<DeptTimetableDTO>>>> GetTimetable(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<DeptTimetableDTO>>.Fail(
                    "Department not found"));

            var data = await _context.Timetables
                .Where(t => t.DeptId == id
                         && t.IsVisible
                         && t.IsLatest)
                .OrderBy(t => t.Semester)
                .Select(t => new DeptTimetableDTO
                {
                    Id = t.Id,
                    Year = t.Year,
                    SemesterType = t.SemesterType,
                    Semester = t.Semester,
                    FilePath = t.FilePath,
                    UploadedDate = t.UploadedDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(ApiResponse<List<DeptTimetableDTO>>.Ok(data));
        }

        // GET /api/departments/{id}/intake
        // Full year-wise history
        [HttpGet("{id}/intake")]
        public async Task<ActionResult<ApiResponse<List<DeptIntakeDTO>>>> GetIntake(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<DeptIntakeDTO>>.Fail(
                    "Department not found"));

            var intakes = await _context.ProgramIntakes
                .Where(p => p.DeptId == id && p.IsVisible)
                .OrderByDescending(p => p.IntakeYear)
                .ToListAsync();

            var data = intakes.Select((p, idx) => new DeptIntakeDTO
            {
                IntakeYear = p.IntakeYear,
                Intake = p.Intake,
                IsLatest = idx == 0
            }).ToList();

            return Ok(ApiResponse<List<DeptIntakeDTO>>.Ok(data));
        }

        // GET /api/departments/{id}/notices
        // Only active notices (within validity window)
        [HttpGet("{id}/notices")]
        public async Task<ActionResult<ApiResponse<List<DeptNoticeDTO>>>> GetNotices(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<DeptNoticeDTO>>.Fail(
                    "Department not found"));

            var now = DateTime.Now;

            var data = await _context.DeptNotices
                .Where(n => n.DeptId == id
                    && n.IsVisible
                    && (!n.ValidFrom.HasValue || n.ValidFrom <= now)
                    && (!n.ValidTo.HasValue || n.ValidTo >= now))
                .OrderBy(n => n.DisplayOrder)
                .Select(n => new DeptNoticeDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    Description = n.Description,
                    FilePath = n.FilePath,
                    FileType = n.FileType,
                    ExternalLink = n.ExternalLink,
                    ValidFrom = n.ValidFrom.HasValue
                        ? n.ValidFrom.Value.ToString("yyyy-MM-dd") : null,
                    ValidTo = n.ValidTo.HasValue
                        ? n.ValidTo.Value.ToString("yyyy-MM-dd") : null,
                    PostedBy = n.PostedBy,
                    DisplayOrder = n.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<DeptNoticeDTO>>.Ok(data));
        }
        // GET /api/departments/{id}/activities
        [HttpGet("{id}/activities")]
        public async Task<ActionResult<ApiResponse<List<ActivityDTO>>>> GetActivities(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<ActivityDTO>>.Fail(
                    "Department not found"));

            var activities = await _context.Activities
                .Include(a => a.Images.OrderBy(i => i.DisplayOrder))
                .Include(a => a.Files.OrderBy(f => f.DisplayOrder))
                .Where(a => a.DeptId == id && a.IsVisible)
                .OrderByDescending(a => a.EventDate)
                .ToListAsync();

            var data = activities.Select(a => new ActivityDTO
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                EventDate = a.EventDate.HasValue
                    ? a.EventDate.Value.ToString("yyyy-MM-dd") : null,
                EventTime = a.EventTime,
                Year = a.Year,
                TargetStudents = a.TargetStudents,
                Keywords = a.Keywords,
                ExternalLink = a.ExternalLink,
                Link = !string.IsNullOrEmpty(a.ExternalLink)
                    ? a.ExternalLink
                    : (!string.IsNullOrEmpty(a.ControllerName)
                       && !string.IsNullOrEmpty(a.ActionName)
                        ? $"/{a.ControllerName}/{a.ActionName}" : null),
                Images = a.Images.Select(img => new ActivityImageDTO
                {
                    ImagePath = img.ImagePath,
                    DisplayOrder = img.DisplayOrder
                }).ToList(),
                Files = a.Files.Select(f => new ActivityFileDTO
                {
                    Title = f.Title,
                    FilePath = f.FilePath,
                    FileType = f.FileType,
                    DisplayOrder = f.DisplayOrder
                }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<ActivityDTO>>.Ok(data));
        }
        // GET /api/departments/{id}/achievements
        [HttpGet("{id}/achievements")]
        public async Task<ActionResult<ApiResponse<List<AchievementDTO>>>> GetAchievements(int id)
        {
            var deptExists = await _context.Departments
                .AnyAsync(d => d.DeptId == id && d.IsActive);

            if (!deptExists)
                return NotFound(ApiResponse<List<AchievementDTO>>.Fail(
                    "Department not found"));

            var data = await _context.Achievements
                .Where(a => a.DeptId == id && a.IsVisible)
                .OrderByDescending(a => a.Year)
                .ThenByDescending(a => a.Date)
                .Select(a => new AchievementDTO
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    ImagePath = a.ImagePath,
                    Date = a.Date,
                    Year = a.Year,
                   // TypeName = TypeName(a.Type),
                    DeptId = a.DeptId,
                    DeptName = a.DeptName,
                    CommitteeId = a.CommitteeId
                })
                .ToListAsync();

            return Ok(ApiResponse<List<AchievementDTO>>.Ok(data));
        }

    }
}