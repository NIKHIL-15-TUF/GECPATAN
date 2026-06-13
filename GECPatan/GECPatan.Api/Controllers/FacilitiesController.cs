using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/facilities")]
    public class FacilitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FacilitiesController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════
        // GET /api/facilities
        // ════════════════════════════════════════════════
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FacilityListDTO>>>> GetAll()
        {
            var facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            var ids = facilities.Select(f => f.Id).ToList();

            var memberCounts = await _context.FacilityMembers
                .Where(m => ids.Contains(m.FacilityId))
                .GroupBy(m => m.FacilityId)
                .Select(g => new { FacilityId = g.Key, Count = g.Count() })
                .ToListAsync();

            var data = facilities.Select(f => new FacilityListDTO
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                TitleImagePath = f.TitleImagePath,
                DisplayOrder = f.DisplayOrder,
                MemberCount = memberCounts
                    .FirstOrDefault(m => m.FacilityId == f.Id)?.Count ?? 0
            }).ToList();

            return Ok(ApiResponse<List<FacilityListDTO>>.Ok(data));
        }

        // ════════════════════════════════════════════════
        // GET /api/facilities/{id}
        // ════════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FacilityDetailDTO>>> GetById(int id)
        {
            var f = await _context.Facilities
                .Include(x => x.BannerImages.OrderBy(b => b.DisplayOrder))
                .Include(x => x.Visions.OrderBy(v => v.DisplayOrder))
                .Include(x => x.Missions.OrderBy(m => m.DisplayOrder))
                .Include(x => x.Members.OrderBy(m => m.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (f == null)
                return NotFound(ApiResponse<FacilityDetailDTO>.Fail(
                    "Facility not found"));

            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Facility
                         && s.PageId == id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var data = new FacilityDetailDTO
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                About = f.About,
                TitleImagePath = f.TitleImagePath,

                BannerImages = f.BannerImages.Select(b => b.ImagePath).ToList(),
                Visions = f.Visions.Select(v => v.VisionText).ToList(),
                Missions = f.Missions.Select(m => m.MissionText).ToList(),

                Members = f.Members.Select(m => new FacilityMemberDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Position = m.Position,
                    Department = m.Department,
                    ImagePath = m.ImagePath,
                    DisplayOrder = m.DisplayOrder
                }).ToList(),

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
                        .OrderBy(file => file.DisplayOrder)
                        .Select(file => new DynamicSectionFileDTO
                        {
                            FilePath = file.FilePath,
                            Title = file.Title,
                            FileType = file.FileType,
                            DisplayOrder = file.DisplayOrder
                        }).ToList()
                }).ToList()
            };

            return Ok(ApiResponse<FacilityDetailDTO>.Ok(data));
        }
    }
}