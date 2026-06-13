using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/committees")]
    public class CommitteesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommitteesController(ApplicationDbContext context)
            => _context = context;
        // GET /api/committees
        // All active committees (list)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CommitteeListDTO>>>> GetAll()
        {
            var committees = await _context.CampusCommittees
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var ids = committees.Select(c => c.Id).ToList();

            var memberCounts = await _context.CommitteeMembers
                .Where(m => ids.Contains(m.CommitteeId))
                .GroupBy(m => m.CommitteeId)
                .Select(g => new { CommitteeId = g.Key, Count = g.Count() })
                .ToListAsync();

            var activityCounts = await _context.Activities
                .Where(a => a.CommitteeId.HasValue
                         && ids.Contains(a.CommitteeId.Value)
                         && a.IsVisible)
                .GroupBy(a => a.CommitteeId)
                .Select(g => new { CommitteeId = g.Key, Count = g.Count() })
                .ToListAsync();

            var data = committees.Select(c => new CommitteeListDTO
            {
                Id = c.Id,
                Title = c.Title,
                Tagline = c.Tagline,
                TitleImagePath = c.TitleImagePath,
                DisplayOrder = c.DisplayOrder,
                MemberCount = memberCounts
                    .FirstOrDefault(m => m.CommitteeId == c.Id)?.Count ?? 0,
                ActivityCount = activityCounts
                    .FirstOrDefault(a => a.CommitteeId == c.Id)?.Count ?? 0
            }).ToList();

            return Ok(ApiResponse<List<CommitteeListDTO>>.Ok(data));
        }
        // GET /api/committees/{id}
        // Full committee detail
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CommitteeDetailDTO>>> GetById(int id)
        {
            var c = await _context.CampusCommittees
                .Include(x => x.Visions.OrderBy(v => v.DisplayOrder))
                .Include(x => x.Missions.OrderBy(m => m.DisplayOrder))
                .Include(x => x.Objectives.OrderBy(o => o.DisplayOrder))
                .Include(x => x.SubObjectives.OrderBy(s => s.DisplayOrder))
                .Include(x => x.Members.OrderBy(m => m.DisplayOrder))
                .Include(x => x.AdditionalMemberGroups.OrderBy(g => g.DisplayOrder))
                    .ThenInclude(g => g.Members.OrderBy(m => m.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (c == null)
                return NotFound(ApiResponse<CommitteeDetailDTO>.Fail(
                    "Committee not found"));

            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Committee
                         && s.PageId == id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var data = new CommitteeDetailDTO
            {
                Id = c.Id,
                Title = c.Title,
                About = c.About,
                Tagline = c.Tagline,
                Measures = c.Measures,
                Message = c.Message,

                TitleImagePath = c.TitleImagePath,
                TitleImageCSSClass = c.TitleImageCSSClass,
                MeasureImagePath = c.MeasureImagePath,
                SubObjImagePath = c.SubObjImagePath,
                BulletPointsImagePath = c.BulletPointsImagePath,
                PageFlyerPath = c.PageFlyerPath,

                BlogLink = c.BlogLink,
                Link = c.Link,
                Account = c.Account,
                NationalTaskForce = c.NationalTaskForce,

                ShowDocument = c.ShowDocument,
                TableView = c.TableView,

                TabAbout = c.TabAbout,
                TabVisionMission = c.TabVisionMission,
                TabObjectives = c.TabObjectives,
                TabMembers = c.TabMembers,
                TabActivities = c.TabActivities,
                TabDocuments = c.TabDocuments,
                TabLink = c.TabLink,

                Visions = c.Visions.Select(v => v.VisionText).ToList(),
                Missions = c.Missions.Select(m => m.MissionText).ToList(),
                Objectives = c.Objectives.Select(o => o.ObjectiveText).ToList(),
                SubObjectives = c.SubObjectives.Select(s => s.SubObjectiveText).ToList(),

                Members = c.Members.Select(m => new CommitteeMemberDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Position = m.Position,
                    ImagePath = m.ImagePath,
                    Department = m.Department,
                    DisplayOrder = m.DisplayOrder
                }).ToList(),

                AdditionalMemberGroups = c.AdditionalMemberGroups
                    .Select(g => new AdditionalMemberGroupDTO
                    {
                        Id = g.Id,
                        GroupTitle = g.GroupTitle,
                        DisplayOrder = g.DisplayOrder,
                        Members = g.Members.Select(m => new AdditionalMemberDetailDTO
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Position = m.Position,
                            ImagePath = m.ImagePath,
                            Department = m.Department,
                            DisplayOrder = m.DisplayOrder
                        }).ToList()
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

            return Ok(ApiResponse<CommitteeDetailDTO>.Ok(data));
        }

        // GET /api/committees/{id}/members
        // Main members + additional member groups
        [HttpGet("{id}/members")]
        public async Task<ActionResult<ApiResponse<object>>> GetMembers(int id)
        {
            var exists = await _context.CampusCommittees
                .AnyAsync(c => c.Id == id && c.IsActive);

            if (!exists)
                return NotFound(ApiResponse<object>.Fail("Committee not found"));

            var members = await _context.CommitteeMembers
                .Where(m => m.CommitteeId == id)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new CommitteeMemberDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Position = m.Position,
                    ImagePath = m.ImagePath,
                    Department = m.Department,
                    DisplayOrder = m.DisplayOrder
                })
                .ToListAsync();

            var groups = await _context.AdditionalMemberGroups
                .Include(g => g.Members.OrderBy(m => m.DisplayOrder))
                .Where(g => g.CommitteeId == id)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new AdditionalMemberGroupDTO
                {
                    Id = g.Id,
                    GroupTitle = g.GroupTitle,
                    DisplayOrder = g.DisplayOrder,
                    Members = g.Members.Select(m => new AdditionalMemberDetailDTO
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Position = m.Position,
                        ImagePath = m.ImagePath,
                        Department = m.Department,
                        DisplayOrder = m.DisplayOrder
                    }).ToList()
                })
                .ToListAsync();

            var data = new
            {
                members,
                additionalGroups = groups
            };

            return Ok(ApiResponse<object>.Ok(data));
        }

        // GET /api/committees/{id}/activities
        [HttpGet("{id}/activities")]
        public async Task<ActionResult<ApiResponse<List<ActivityDTO>>>> GetActivities(int id)
        {
            var exists = await _context.CampusCommittees
                .AnyAsync(c => c.Id == id && c.IsActive);

            if (!exists)
                return NotFound(ApiResponse<List<ActivityDTO>>.Fail(
                    "Committee not found"));

            var activities = await _context.Activities
                .Include(a => a.Images.OrderBy(i => i.DisplayOrder))
                .Include(a => a.Files.OrderBy(f => f.DisplayOrder))
                .Where(a => a.CommitteeId == id && a.IsVisible)
                .OrderByDescending(a => a.EventDate)
                .ToListAsync();

            var data = activities.Select(a => new ActivityDTO
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                EventDate = a.EventDate.HasValue
                    ? a.EventDate.Value.ToString("yyyy-MM-dd") : null,
                EventTime = a.EventTime,
                Year = a.Year,
                TargetStudents = a.TargetStudents,
                Keywords = a.Keywords,
                ExternalLink = a.ExternalLink,
                Link = ResolveActivityLink(a),
                Images = a.Images.Select(img => new ActivityImageDTO
                {
                    ImagePath = img.ImagePath,
                    DisplayOrder = img.DisplayOrder
                }).ToList(),
                Files = a.Files.Select(f => new ActivityFileDTO
                {
                    Title = f.Title,
                    FilePath = f.FilePath,
                    FileType = f.FileType,
                    DisplayOrder = f.DisplayOrder
                }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<ActivityDTO>>.Ok(data));
        }

        // ── HELPERS ───────────────────────────────────────
        private static string? ResolveActivityLink(Activity a)
        {
            if (!string.IsNullOrEmpty(a.ExternalLink))
                return a.ExternalLink;

            if (!string.IsNullOrEmpty(a.ControllerName)
                && !string.IsNullOrEmpty(a.ActionName))
                return $"/{a.ControllerName}/{a.ActionName}";

            return null;
        }
    }
}