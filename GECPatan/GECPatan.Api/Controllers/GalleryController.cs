using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/gallery")]
    public class GalleryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GalleryController(ApplicationDbContext context)
            => _context = context;
        // GET /api/gallery
        // All images flat list
        // Optional: ?category=Sports to filter by category
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<GalleryImageDTO>>>> GetAll(
            [FromQuery] string? category = null)
        {
            var query = _context.GalleryImages
                .Where(g => g.IsVisible);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.Category == category);

            var data = await query
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new GalleryImageDTO
                {
                    Id = g.Id,
                    ImagePath = g.ImagePath,
                    Caption = g.Caption,
                    Category = g.Category,
                    DisplayOrder = g.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<GalleryImageDTO>>.Ok(data));
        }

        // GET /api/gallery/categories
        // Grouped by category with cover image + count
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<GalleryCategoryDTO>>>> GetCategories()
        {
            var images = await _context.GalleryImages
                .Where(g => g.IsVisible)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new GalleryImageDTO
                {
                    Id = g.Id,
                    ImagePath = g.ImagePath,
                    Caption = g.Caption,
                    Category = g.Category,
                    DisplayOrder = g.DisplayOrder
                })
                .ToListAsync();

            // Group by category (null category goes into "General")
            var grouped = images
                .GroupBy(g => g.Category ?? "General")
                .OrderBy(g => g.Key)
                .Select(g => new GalleryCategoryDTO
                {
                    Category = g.Key,
                    ImageCount = g.Count(),
                    CoverImage = g.First().ImagePath,
                    Images = g.ToList()
                })
                .ToList();

            return Ok(ApiResponse<List<GalleryCategoryDTO>>.Ok(grouped));
        }
    }
}