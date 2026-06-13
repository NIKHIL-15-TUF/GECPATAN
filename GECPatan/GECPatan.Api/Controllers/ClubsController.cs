using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/clubs")]
    public class ClubsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClubsController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════
        // GET /api/clubs
        // ════════════════════════════════════════════════
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StudentClubListDTO>>>> GetAll()
        {
            var clubs = await _context.StudentClubs
                .Include(c => c.Images.OrderBy(i => i.DisplayOrder))
                .Where(c => c.IsVisible)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var data = clubs.Select(c => new StudentClubListDTO
            {
                Id = c.Id,
                Title = c.Title,
                CoverImage = c.Images.FirstOrDefault()?.ImagePath,
                DisplayOrder = c.DisplayOrder,
                Link = ResolveClubLink(c)
            }).ToList();

            return Ok(ApiResponse<List<StudentClubListDTO>>.Ok(data));
        }

        // ════════════════════════════════════════════════
        // GET /api/clubs/{id}
        // ════════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StudentClubDetailDTO>>> GetById(int id)
        {
            var c = await _context.StudentClubs
                .Include(x => x.Images.OrderBy(i => i.DisplayOrder))
                .Include(x => x.Members.OrderBy(m => m.DisplayOrder))
                .Include(x => x.Objectives.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsVisible);

            if (c == null)
                return NotFound(ApiResponse<StudentClubDetailDTO>.Fail(
                    "Student club not found"));
            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Custom
                         && s.PageId == id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
            var data = new StudentClubDetailDTO
            {
                Id = c.Id,
                Title = c.Title,
                About = c.About,
                BlogLink = c.BlogLink,
                Link = ResolveClubLink(c),

                Images = c.Images.Select(img => new ClubImageDTO
                {
                    ImagePath = img.ImagePath,
                    Caption = img.Caption,
                    DisplayOrder = img.DisplayOrder
                }).ToList(),

                Members = c.Members.Select(m => new ClubMemberDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Position = m.Position,
                    Department = m.Department,
                    ImagePath = m.ImagePath,
                    DisplayOrder = m.DisplayOrder
                }).ToList(),

                Objectives = c.Objectives
                    .Select(o => o.ObjectiveText).ToList(),
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

            return Ok(ApiResponse<StudentClubDetailDTO>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        private static string? ResolveClubLink(StudentClub c)
        {
            if (c.IsDynamic
                && !string.IsNullOrEmpty(c.ControllerName)
                && !string.IsNullOrEmpty(c.ActionName))
            {
                return c.DynamicId.HasValue
                    ? $"/{c.ControllerName}/{c.ActionName}/{c.DynamicId}"
                    : $"/{c.ControllerName}/{c.ActionName}";
            }

            return $"/clubs/{c.Id}";
        }
    }
}