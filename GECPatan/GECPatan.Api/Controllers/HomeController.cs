using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        // The HORIZONTAL scrolling ticker directly below the slider.
        // Only Admin > Marquee items explicitly flagged HorizontalMarquee = true
        // show here. Everything else (HorizontalMarquee = false, plus any
        // NewsItem flagged ShowInMarquee) shows in GET /api/home/updates
        // (the vertical "UPDATES" list) instead -- see GetUpdates() below.
        [HttpGet("marquee")]
        public async Task<ActionResult<ApiResponse<List<MarqueeDTO>>>> GetMarquee()
        {
            var now = DateTime.Now;

            var items = await _context.Marquees
                .Where(m => m.IsVisible
                    && m.HorizontalMarquee
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
                FilePath = m.LinkType == "file" ? m.FilePath : null,
                DisplayOrder = m.DisplayOrder
            }).ToList();

            return Ok(ApiResponse<List<MarqueeDTO>>.Ok(data));
        }

        // GET /api/home/updates?take=20
        // The vertical "UPDATES" list. Merges:
        //   - Marquee items where HorizontalMarquee == false (the default --
        //     most marquee items land here, not in the ticker)
        //   - NewsItem items explicitly flagged ShowInMarquee == true
        // Ordered newest-first by each source's own natural date
        // (Marquee.CreatedDate, NewsItem.PublishDate).
        [HttpGet("updates")]
        public async Task<ActionResult<ApiResponse<List<UpdateItemDTO>>>> GetUpdates(
            [FromQuery] int take = 20)
        {
            if (take < 1 || take > 100) take = 20;

            var now = DateTime.Now;

            var marqueeItems = await _context.Marquees
                .Where(m => m.IsVisible
                    && !m.HorizontalMarquee
                    && (!m.ValidFrom.HasValue || m.ValidFrom <= now)
                    && (!m.ValidTo.HasValue || m.ValidTo >= now))
                .ToListAsync();

            var updates = marqueeItems
                .Select(m => new
                {
                    SortDate = m.CreatedDate,
                    Dto = new UpdateItemDTO
                    {
                        Id = m.Id,
                        Title = m.Title,
                        LinkType = m.LinkType,
                        Link = ResolveMarqueeLink(m),
                        FilePath = m.LinkType == "file" ? m.FilePath : null,
                        Source = "marquee"
                    }
                })
                .ToList();

            var newsItems = await _context.NewsItems
                .Where(n => n.IsVisible && n.ShowInMarquee)
                .ToListAsync();

            var newsUpdates = newsItems.Select(n => new
            {
                SortDate = n.PublishDate ?? n.CreatedDate,
                Dto = new UpdateItemDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    LinkType = "internal",
                    Link = $"/news/{n.Id}",
                    FilePath = null,
                    Source = "news"
                }
            });

            var data = updates
                .Concat(newsUpdates)
                .OrderByDescending(u => u.SortDate)
                .Take(take)
                .Select(u => u.Dto)
                .ToList();

            return Ok(ApiResponse<List<UpdateItemDTO>>.Ok(data));
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
        // Unchanged/unfiltered -- general "latest news" feed, independent of
        // ShowInMarquee. ShowInMarquee only controls whether an item ALSO
        // appears in GET /api/home/updates (see GetUpdates above); it does
        // not remove the item from here.
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
            var settings = await _context.SiteSettings.ToListAsync();

            string? Cfg(string key)
            {
                return settings.FirstOrDefault(x => x.Key == key)?.Value;
            }
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
                LatestHighestPackage = latestPlacement?.HighestPackage,
                Feature1Text = Cfg("Home.Feature1.Text"),
                Feature1Icon = Cfg("Home.Feature1.Icon"),

                Feature2Text = Cfg("Home.Feature2.Text"),
                Feature2Icon = Cfg("Home.Feature2.Icon"),

                Feature3Text = Cfg("Home.Feature3.Text"),
                Feature3Icon = Cfg("Home.Feature3.Icon"),

                Feature4Text = Cfg("Home.Feature4.Text"),
                Feature4Icon = Cfg("Home.Feature4.Icon"),
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
                Address = Get("Contact.Address"),
                MapEmbedUrl = Get("Contact.MapEmbedUrl"),
                MapLatitude = Get("Contact.MapLatitude"),
                MapLongitude = Get("Contact.MapLongitude")
            };

            return Ok(ApiResponse<SiteSettingsDTO>.Ok(data));
        }

        // GET /api/home/principal
        [HttpGet("principal")]
        public async Task<ActionResult<ApiResponse<PrincipalMessageDTO>>>
            GetPrincipalMessage()
        {
            var p = await _context.Principals
                .Where(x => x.IsActive && !x.IsDeleted)
                .Select(x => new PrincipalMessageDTO
                {
                    Name = x.Name,
                    Designation = x.Designation,
                    PhotoPath = x.PhotoPath,
                    Message = x.Message,
                    Institute = "Government Engineering College, Patan"
                })
                .FirstOrDefaultAsync();

            if (p == null)
                return NotFound(ApiResponse<PrincipalMessageDTO>.Fail(
                    "No active principal profile found."));

            return Ok(ApiResponse<PrincipalMessageDTO>.Ok(p));
        }

        // GET /api/home/activities?take=10
        [HttpGet("activities")]
        public async Task<ActionResult<ApiResponse<List<HomeActivityDTO>>>> GetLatestActivities(
            [FromQuery] int take = 10)
        {
            if (take < 1 || take > 50) take = 10;

            var items = await _context.Activities
                .Include(a => a.Images.OrderBy(i => i.DisplayOrder))
                .Where(a => a.IsVisible)
                .OrderByDescending(a => a.EventDate)
                .Take(take)
                .ToListAsync();

            var data = items.Select(a => new HomeActivityDTO
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                EventDate = a.EventDate.HasValue
                    ? a.EventDate.Value.ToString("yyyy-MM-dd") : null,
                Year = a.Year,
                CommitteeId = a.CommitteeId,
                DeptId = a.DeptId,
                ClubId = a.ClubId,
                Link = a.CommitteeId.HasValue
                    ? $"/committees/{a.CommitteeId}/activities"
                    : a.DeptId.HasValue
                        ? $"/departments/{a.DeptId}/activities"
                        : a.ClubId.HasValue
                            ? $"/clubs/{a.ClubId}/activities"
                            : $"/activities/{a.Id}",
                ThumbnailPath = a.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .FirstOrDefault()
            }).ToList();

            return Ok(ApiResponse<List<HomeActivityDTO>>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        // Resolves Link for internal/dynamic/external types only. Returns
        // null for "file" and "none" -- file links are exposed separately
        // via FilePath (see MarqueeDTO/UpdateItemDTO), since a raw
        // Api-relative file path needs Api:BaseUrl resolution on the
        // consumer side, not a same-origin href.
        private static string? ResolveMarqueeLink(Marquee m)
        {
            return m.LinkType switch
            {
                "internal" => $"/{m.ControllerName}/{m.ActionName}",
                "dynamic" => $"/{m.ControllerName}/{m.ActionName}/{m.DynamicId}",
                "external" => m.ExternalLink,
                _ => null
            };
        }
    }
}
