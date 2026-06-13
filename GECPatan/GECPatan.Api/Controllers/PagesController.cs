using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/pages")]
    public class PagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PagesController(ApplicationDbContext context)
            => _context = context;
        // GET /api/pages/{slug}
        // Content page by slug, with Dynamic Sections
        [HttpGet("{slug}")]
        public async Task<ActionResult<ApiResponse<ContentPageDTO>>> GetBySlug(string slug)
        {
            var page = await _context.ContentPages
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsVisible);

            if (page == null)
                return NotFound(ApiResponse<ContentPageDTO>.Fail(
                    "Page not found"));

            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Custom
                         && s.PageId == page.Id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var data = new ContentPageDTO
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                HtmlContent = page.HtmlContent,
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

            return Ok(ApiResponse<ContentPageDTO>.Ok(data));
        }
    }
}
