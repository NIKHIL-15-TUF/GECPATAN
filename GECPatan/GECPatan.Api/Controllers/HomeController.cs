using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
            => _context = context;

        // GET /api/home/slider
        [HttpGet("slider")]
        public async Task<ActionResult<ApiResponse<List<SliderDTO>>>> GetSliders()
        {
            var data = await _context.Sliders
                .Where(s => s.IsVisible)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SliderDTO
                {
                    Id = s.Id,
                    ImagePath = s.ImagePath,
                    H3Text = s.H3Text,
                    H4Text = s.H4Text,
                    H5Text = s.H5Text,
                    Anchor1Text = s.Anchor1Text,
                    Anchor1Link = s.Anchor1Link,
                    Anchor2Text = s.Anchor2Text,
                    Anchor2Link = s.Anchor2Link,
                    DisplayOrder = s.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<SliderDTO>>.Ok(data));
        }
        // GET /api/home/marquee
        [HttpGet("marquee")]
        public async Task<ActionResult<ApiResponse<List<MarqueeDTO>>>> GetMarquee()
        {
            var now = DateTime.Now;

            var items = await _context.Marquees
                .Where(m => m.IsVisible
                    && (!m.ValidFrom.HasValue || m.ValidFrom <= now)
                    && (!m.ValidTo.HasValue || m.ValidTo >= now))
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var data = items.Select(m => new MarqueeDTO
            {
                Id = m.Id,
                Title = m.Title,
                LinkType = m.LinkType,
                Link = ResolveMarqueeLink(m),
                FilePath = m.FilePath,
                DisplayOrder = m.DisplayOrder
            }).ToList();

            return Ok(ApiResponse<List<MarqueeDTO>>.Ok(data));
        }

        // GET /api/home/testimonials
        [HttpGet("testimonials")]
        public async Task<ActionResult<ApiResponse<List<TestimonialDTO>>>> GetTestimonials()
        {
            var data = await _context.Testimonials
                .Where(t => t.IsVisible)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new TestimonialDTO
                {
                    Id = t.Id,
                    StudentName = t.StudentName,
                    Department = t.Department,
                    PassoutYear = t.PassoutYear,
                    TestimonialText = t.TestimonialText,
                    DisplayOrder = t.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TestimonialDTO>>.Ok(data));
        }

        // GET /api/home/toprecruiters
        [HttpGet("toprecruiters")]
        public async Task<ActionResult<ApiResponse<List<TopRecruiterDTO>>>> GetTopRecruiters()
        {
            var data = await _context.TopRecruiters
                .Where(r => r.IsVisible)
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new TopRecruiterDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    LogoPath = r.LogoPath,
                    DisplayOrder = r.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TopRecruiterDTO>>.Ok(data));
        }

        // GET /api/home/news?take=5
        [HttpGet("news")]
        public async Task<ActionResult<ApiResponse<List<HomeNewsDTO>>>> GetLatestNews(
            [FromQuery] int take = 5)
        {
            var data = await _context.NewsItems
                .Where(n => n.IsVisible)
                .OrderByDescending(n => n.PublishDate)
                .Take(take)
                .Select(n => new HomeNewsDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    PublishDate = n.PublishDate.HasValue
                        ? n.PublishDate.Value.ToString("yyyy-MM-dd")
                        : null,
                    ShowInMarquee = n.ShowInMarquee,
                    Link = $"/news/{n.Id}"
                })
                .ToListAsync();

            return Ok(ApiResponse<List<HomeNewsDTO>>.Ok(data));
        }

        // GET /api/home/stats
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<HomeStatsDTO>>> GetStats()
        {
            var latestPlacement = await _context.PlacementStatistics
                .OrderByDescending(p => p.Year)
                .FirstOrDefaultAsync();

            var data = new HomeStatsDTO
            {
                DepartmentCount = await _context.Departments
                    .CountAsync(d => d.IsActive),
                FacultyCount = await _context.Faculties
                    .CountAsync(f => f.IsActive),
                StudentClubCount = await _context.StudentClubs.CountAsync(),
                LatestPlacementYear = latestPlacement != null
                    && int.TryParse(latestPlacement.Year, out var yr)
                        ? yr : null,
                LatestTotalPlaced = latestPlacement?.TotalPlaced,
                LatestHighestPackage = latestPlacement?.HighestPackage
            };

            return Ok(ApiResponse<HomeStatsDTO>.Ok(data));
        }

        // GET /api/home/settings
        [HttpGet("settings")]
        public async Task<ActionResult<ApiResponse<SiteSettingsDTO>>> GetSettings()
        {
            var all = await _context.SiteSettings.ToListAsync();
            string? Get(string key) =>
                all.FirstOrDefault(s => s.Key == key)?.Value;

            var data = new SiteSettingsDTO
            {
                Vision = Get("HomePage.Vision"),
                Mission = Get("HomePage.Mission"),
                PrincipalName = Get("Principal.Name"),
                PrincipalDesignation = Get("Principal.Designation"),
                PrincipalMessage = Get("Principal.Message"),
                PrincipalPhoto = Get("Principal.Photo"),
                EstablishedYear = Get("College.EstablishedYear"),
                CollegeTagline = Get("College.Tagline"),
                FacebookUrl = Get("Social.Facebook"),
                TwitterUrl = Get("Social.Twitter"),
                YouTubeUrl = Get("Social.YouTube"),
                LinkedInUrl = Get("Social.LinkedIn"),
                InstagramUrl = Get("Social.Instagram"),
                Phone = Get("Contact.Phone"),
                Email = Get("Contact.Email"),
                Address = Get("Contact.Address")
            };

            return Ok(ApiResponse<SiteSettingsDTO>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        private static string? ResolveMarqueeLink(
            GECPatan.Core.Models.Domain.Marquee m)
        {
            return m.LinkType switch
            {
                "internal" => $"/{m.ControllerName}/{m.ActionName}",
                "dynamic" => $"/{m.ControllerName}/{m.ActionName}/{m.DynamicId}",
                "external" => m.ExternalLink,
                "file" => m.FilePath,
                _ => null
            };
        }
    }
}