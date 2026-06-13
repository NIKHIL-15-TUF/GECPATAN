using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/achievements")]
    public class AchievementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AchievementsController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════
        // GET /api/achievements
        // Optional filters: ?deptId=1  ?committeeId=2  ?type=1
        // type: 1=Academic, 2=Sports, 3=Cultural, 4=NSS, 5=Other
        // ════════════════════════════════════════════════
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AchievementDTO>>>> GetAll(
            [FromQuery] int? deptId = null,
            [FromQuery] int? committeeId = null,
            [FromQuery] int? type = null)
        {
            var query = _context.Achievements
                .Where(a => a.IsVisible);

            if (deptId.HasValue)
                query = query.Where(a => a.DeptId == deptId.Value);

            if (committeeId.HasValue)
                query = query.Where(a => a.CommitteeId == committeeId.Value);

            if (type.HasValue)
                query = query.Where(a => a.Type == type.Value);

            var data = await query
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
                    TypeName = TypeName(a.Type),
                    DeptId = a.DeptId,
                    DeptName = a.DeptName,
                    CommitteeId = a.CommitteeId
                })
                .ToListAsync();

            return Ok(ApiResponse<List<AchievementDTO>>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        private static string TypeName(int type) => type switch
        {
            1 => "Academic",
            2 => "Sports",
            3 => "Cultural",
            4 => "NSS",
            5 => "Other",
            _ => "Other"
        };
    }
}