using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ActivitiesController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════
        // GET /api/activities
        // All activities — paged + filterable
        // Query params:
        //   ?committeeId=1   filter by committee
        //   ?deptId=1        filter by department
        //   ?clubId=1        filter by student club
        //   ?year=2025       filter by year
        //   ?page=1          page number (default 1)
        //   ?pageSize=20     items per page (max 50)
        // ════════════════════════════════════════════════
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ActivityDTO>>>> GetAll(
            [FromQuery] int? committeeId = null,
            [FromQuery] int? deptId = null,
            [FromQuery] int? clubId = null,
            [FromQuery] int? year = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            var query = _context.Activities
                .Include(a => a.Images.OrderBy(i => i.DisplayOrder))
                .Include(a => a.Files.OrderBy(f => f.DisplayOrder))
                .Where(a => a.IsVisible);

            if (committeeId.HasValue)
                query = query.Where(a => a.CommitteeId == committeeId.Value);

            if (deptId.HasValue)
                query = query.Where(a => a.DeptId == deptId.Value);

            if (clubId.HasValue)
                query = query.Where(a => a.ClubId == clubId.Value);

            if (year.HasValue)
                query = query.Where(a => a.Year == year.Value
                    || (a.EventDate.HasValue
                        && a.EventDate.Value.Year == year.Value));

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.EventDate)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = items.Select(a => MapToDTO(a)).ToList();

            return Ok(ApiResponse<List<ActivityDTO>>.Ok(
                data, "OK", total));
        }

        // ════════════════════════════════════════════════
        // GET /api/activities/{id}
        // Single activity detail
        // ════════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ActivityDTO>>> GetById(int id)
        {
            var a = await _context.Activities
                .Include(x => x.Images.OrderBy(i => i.DisplayOrder))
                .Include(x => x.Files.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsVisible);

            if (a == null)
                return NotFound(ApiResponse<ActivityDTO>.Fail(
                    "Activity not found"));

            return Ok(ApiResponse<ActivityDTO>.Ok(MapToDTO(a)));
        }

        // ════════════════════════════════════════════════
        // GET /api/activities/years
        // Distinct years (for frontend year filter dropdown)
        // ════════════════════════════════════════════════
        [HttpGet("years")]
        public async Task<ActionResult<ApiResponse<List<int>>>> GetYears()
        {
            var years = await _context.Activities
                .Where(a => a.IsVisible && a.Year.HasValue)
                .Select(a => a.Year!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return Ok(ApiResponse<List<int>>.Ok(years));
        }

        // ── HELPER — map Activity to ActivityDTO ──────────
        private static ActivityDTO MapToDTO(
            GECPatan.Core.Models.Domain.Activity a) => new()
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
                : !string.IsNullOrEmpty(a.ControllerName)
                    && !string.IsNullOrEmpty(a.ActionName)
                    ? $"/{a.ControllerName}/{a.ActionName}"
                    : $"/activities/{a.Id}",
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
            };
    }
}