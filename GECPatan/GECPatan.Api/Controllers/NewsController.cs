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

        //// ── HELPERS ───────────────────────────────────────
        //private static string? ResolveLink(NewsItem n)
        //{
        //    if (!string.IsNullOrEmpty(n.ExternalLink))
        //        return n.ExternalLink;

        //    if (!string.IsNullOrEmpty(n.ControllerName)
        //        && !string.IsNullOrEmpty(n.ActionName))
        //        return $"/{n.ControllerName}/{n.ActionName}";

        //    return $"/news/{n.Id}";
        //}
    }
}