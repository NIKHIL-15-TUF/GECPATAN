using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/news")]
    public class NewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
            => _context = context;

        // GET /api/news?page=1&pageSize=10
        // Paged news list
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NewsListDTO>>>> GetAll(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.NewsItems
                .Where(n => n.IsVisible)
                .OrderByDescending(n => n.PublishDate);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = items.Select(n => new NewsListDTO
            {
                Id = n.Id,
                Title = n.Title,
                ThumbnailPath = n.ThumbnailPath,
                PublishDate = n.PublishDate.HasValue
                    ? n.PublishDate.Value.ToString("yyyy-MM-dd") : null,
                ShowInMarquee = n.ShowInMarquee,
                Link = ResolveLink(n)
            }).ToList();

            return Ok(ApiResponse<List<NewsListDTO>>.Ok(
                data, "OK", total));
        }

        // GET /api/news/{id}
        // News detail with images + files
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<NewsDetailDTO>>> GetById(int id)
        {
            var n = await _context.NewsItems
                .Include(x => x.Images.OrderBy(i => i.DisplayOrder))
                .Include(x => x.Files.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsVisible);

            if (n == null)
                return NotFound(ApiResponse<NewsDetailDTO>.Fail(
                    "News item not found"));

            var data = new NewsDetailDTO
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                BannerImagePath = n.BannerImagePath,
                ThumbnailPath = n.ThumbnailPath,
                PublishDate = n.PublishDate.HasValue
                    ? n.PublishDate.Value.ToString("yyyy-MM-dd") : null,
                ExternalLink = n.ExternalLink,
                Link = ResolveLink(n),
                Images = n.Images.Select(img => new NewsImageDTO
                {
                    ImagePath = img.ImagePath,
                    DisplayOrder = img.DisplayOrder
                }).ToList(),
                Files = n.Files.Select(f => new NewsFileDTO
                {
                    Title = f.Title,
                    FilePath = f.FilePath,
                    FileType = f.FileType,
                    DisplayOrder = f.DisplayOrder
                }).ToList()
            };

            return Ok(ApiResponse<NewsDetailDTO>.Ok(data));
        }

        // GET /api/news/letters
        // News letters list
        [HttpGet("letters")]
        public async Task<ActionResult<ApiResponse<List<NewsLetterDTO>>>> GetLetters()
        {
            var data = await _context.NewsLetters
                .Where(l => l.IsVisible)
                .OrderBy(l => l.DisplayOrder)
                .Select(l => new NewsLetterDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    FilePath = l.FilePath,
                    DownloadName = l.DownloadName,
                    ThumbnailPath = l.ThumbnailPath,
                    DisplayOrder = l.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<NewsLetterDTO>>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        private static string? ResolveLink(NewsItem n)
        {
            if (!string.IsNullOrEmpty(n.ExternalLink))
                return n.ExternalLink;

            if (!string.IsNullOrEmpty(n.ControllerName)
                && !string.IsNullOrEmpty(n.ActionName))
                return $"/{n.ControllerName}/{n.ActionName}";

            return $"/news/{n.Id}";
        }
    }
}