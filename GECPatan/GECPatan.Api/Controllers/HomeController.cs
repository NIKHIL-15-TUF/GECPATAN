using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services;
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
        // Horizontal ticker. Returns only visible marquee items flagged
        // HorizontalMarquee = true and within their validity dates.
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

            var pageSlugs = await GetContentPageSlugsAsync(items);

            var data = items.Select(m => new MarqueeDTO
            {
                Id = m.Id,
                Title = m.Title,
                LinkType = m.LinkType,
                Link = ResolveMarqueeLink(m, pageSlugs),
                IsNew = m.IsNew,
                FilePath = m.LinkType == "file" ? m.FilePath : null,
                DisplayOrder = m.DisplayOrder
            }).ToList();

            return Ok(ApiResponse<List<MarqueeDTO>>.Ok(data));
        }

        // GET /api/home/updates?take=20
        // Vertical updates list. Uses Marquee records only; NewsItems are
        // currently not part of the updates feed.
        [HttpGet("updates")]
        public async Task<ActionResult<ApiResponse<List<UpdateItemDTO>>>> GetUpdates(
            [FromQuery] int take = 20)
        {
            if (take < 1 || take > 100) take = 20;

            var now = DateTime.Now;

            var items = await _context.Marquees
                .Where(m => m.IsVisible
                    && !m.HorizontalMarquee
                    && (!m.ValidFrom.HasValue || m.ValidFrom <= now)
                    && (!m.ValidTo.HasValue || m.ValidTo >= now))
                .OrderByDescending(m => m.CreatedDate)
                .Take(take)
                .ToListAsync();

            var pageSlugs = await GetContentPageSlugsAsync(items);

            var data = items.Select(m => new UpdateItemDTO
            {
                Id = m.Id,
                Title = m.Title,
                LinkType = m.LinkType,
                Link = m.LinkType == "file" ? null : ResolveMarqueeLink(m, pageSlugs),
                FilePath = m.LinkType == "file" ? m.FilePath : null,
                Source = "marquee",
                IsNew = m.IsNew
            }).ToList();

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

        // NewsItems are currently not used by the home API.

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
        // GET /api/home/notable-alumni
        [HttpGet("notable-alumni")]
        public async Task<ActionResult<ApiResponse<List<NotableAlumniDTO>>>>
            GetNotableAlumni()
        {
            var data = await _context.NotableAlumni
                .Where(x => x.IsShow && !x.IsDeleted)
                .OrderBy(x => x.OrderNo)
                .Select(x => new NotableAlumniDTO
                {
                    Id = x.ID,
                    OrderNo = x.OrderNo,
                    Title = x.Title,
                    Image = x.Image,
                    IsIframe = x.IsIframe,
                    IFrame = x.IFrame
                })
                .ToListAsync();

            return Ok(ApiResponse<List<NotableAlumniDTO>>.Ok(data));
        }


        // GET /api/home/video-carousel
        [HttpGet("video-carousel")]
        public async Task<ActionResult<ApiResponse<List<VideoCarouselDTO>>>>
            GetVideoCarousel()
        {
            var data = await _context.VideoCarousel
                .Where(x => x.IsShow && !x.IsDeleted)
                .OrderBy(x => x.OrderNo)
                .Select(x => new VideoCarouselDTO
                {
                    Id = x.ID,
                    OrderNo = x.OrderNo,
                    Title = x.Title,
                    VideoUrl = x.VideoUrl
                })
                .ToListAsync();

            return Ok(ApiResponse<List<VideoCarouselDTO>>.Ok(data));
        }

        // â”€â”€ HELPERS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Resolves Link for internal/dynamic/external types only. Returns
        // null for "file" and "none" -- file links are exposed separately
        // via FilePath (see MarqueeDTO/UpdateItemDTO), since a raw
        // Api-relative file path needs Api:BaseUrl resolution on the
        // consumer side, not a same-origin href.
        private async Task<Dictionary<int, string>> GetContentPageSlugsAsync(
            List<Marquee> items)
        {
            var pageIds = items
                .Where(m => m.LinkType == "dynamic"
                    && m.DynamicType == "ContentPage"
                    && m.DynamicId.HasValue)
                .Select(m => m.DynamicId!.Value)
                .Distinct()
                .ToList();

            if (pageIds.Count == 0)
                return new Dictionary<int, string>();

            return await _context.ContentPages
                .Where(p => pageIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Slug);
        }

        private static string? ResolveMarqueeLink(
            Marquee m,
            IReadOnlyDictionary<int, string> pageSlugs)
        {
            return m.LinkType switch
            {
                "internal" when !string.IsNullOrWhiteSpace(m.ControllerName)
                    && !string.IsNullOrWhiteSpace(m.ActionName)
                    => MenuRoutes.InternalPath(m.ControllerName, m.ActionName),

                "dynamic" when m.DynamicId.HasValue
                    && m.DynamicType == "ContentPage"
                    && pageSlugs.TryGetValue(m.DynamicId.Value, out var slug)
                    => $"/page/{slug}",

                "dynamic" when m.DynamicId.HasValue
                    && m.DynamicType is "Department" or "Committee" or "Facility" or "Club" or "Document"
                    => MenuRoutes.DynamicPath(m.DynamicType, m.DynamicId.Value),

                "external" => m.ExternalLink,
                _ => null
            };
        }
    }
}
