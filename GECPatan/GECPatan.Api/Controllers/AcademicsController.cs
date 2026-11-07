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