using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/academics")]
    public class AcademicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AcademicsController(ApplicationDbContext context)
            => _context = context;
        // GET /api/academics/calendar
        // Optional: ?deptId=1 to filter by department
        // (null DeptId = institute-wide calendar)
        [HttpGet("calendar")]
        public async Task<ActionResult<ApiResponse<List<AcademicCalendarDTO>>>> GetCalendar(
            [FromQuery] int? deptId = null)
        {
            var query = _context.AcademicCalendars
                .Where(c => c.IsVisible);

            if (deptId.HasValue)
                query = query.Where(c => c.DeptId == deptId.Value
                                       || c.DeptId == null);

            var items = await query
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var deptIds = items
                .Where(c => c.DeptId.HasValue)
                .Select(c => c.DeptId!.Value)
                .Distinct()
                .ToList();

            var deptNames = await _context.Departments
                .Where(d => deptIds.Contains(d.DeptId))
                .ToDictionaryAsync(d => d.DeptId, d => d.Name);

            var data = items.Select(c => new AcademicCalendarDTO
            {
                Id = c.Id,
                Title = c.Title,
                FilePath = c.FilePath,
                UploadDate = c.UploadDate,
                DeptId = c.DeptId,
                DeptName = c.DeptId.HasValue
                    && deptNames.ContainsKey(c.DeptId.Value)
                        ? deptNames[c.DeptId.Value]
                        : null,
                DisplayOrder = c.DisplayOrder
            }).ToList();

            return Ok(ApiResponse<List<AcademicCalendarDTO>>.Ok(data));
        }

        // GET /api/academics/research
        // Research grants list
        [HttpGet("research")]
        public async Task<ActionResult<ApiResponse<List<ResearchGrantDTO>>>> GetResearch()
        {
            var data = await _context.ResearchGrants
                .Where(r => r.IsVisible)
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new ResearchGrantDTO
                {
                    Id = r.Id,
                    Title = r.Title,
                    PrincipalInvestigator = r.PrincipalInvestigator,
                    StartDate = r.StartDate.HasValue
                        ? r.StartDate.Value.ToString("yyyy-MM-dd") : null,
                    CompletionDate = r.CompletionDate.HasValue
                        ? r.CompletionDate.Value.ToString("yyyy-MM-dd") : null,
                    Duration = r.Duration,
                    ProjectCost = r.ProjectCost,
                    SponsoringAuthority = r.SponsoringAuthority,
                    DisplayOrder = r.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ResearchGrantDTO>>.Ok(data));
        }
    }
}