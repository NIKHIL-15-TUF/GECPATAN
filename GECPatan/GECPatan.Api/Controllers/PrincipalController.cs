using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
 
namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/principal")]
    public class PrincipalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrincipalController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════
        // GET /api/principal
        // Active principal full profile
        // ════════════════════════════════════════════════
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PrincipalProfileDTO>>> GetActive()
        {
            var p = await _context.Principals
                .Include(x => x.Qualifications.OrderBy(q => q.DisplayOrder))
                .Include(x => x.Experiences.OrderBy(e => e.DisplayOrder))
                .Include(x => x.Publications.OrderBy(pub => pub.DisplayOrder))
                .Include(x => x.BookPublications.OrderBy(b => b.DisplayOrder))
                .Include(x => x.ExpertTalks.OrderBy(t => t.DisplayOrder))
                .Include(x => x.Achievements.OrderBy(a => a.DisplayOrder))
                .Include(x => x.Memberships.OrderBy(m => m.DisplayOrder))
                .FirstOrDefaultAsync(x => x.IsActive && !x.IsDeleted);

            if (p == null)
                return NotFound(ApiResponse<PrincipalProfileDTO>.Fail(
                    "No active principal found"));

            var dynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Principal
                         && s.PageId == p.Id
                         && s.IsVisible
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var data = new PrincipalProfileDTO
            {
                Id = p.Id,
                Name = p.Name,
                Designation = p.Designation,
                Email = p.Email,
                Contact = p.Contact,
                PhotoPath = p.PhotoPath,
                Message = p.Message,
                AreaOfInterest = p.AreaOfInterest,
                DateOfJoiningInstitute = p.DateOfJoiningInstitute.HasValue
                    ? p.DateOfJoiningInstitute.Value.ToString("yyyy-MM-dd") : null,
                DateOfJoiningDept = p.DateOfJoiningDept.HasValue
                    ? p.DateOfJoiningDept.Value.ToString("yyyy-MM-dd") : null,

                Qualifications = p.Qualifications.Select(q => new PrincipalQualificationDTO
                {
                    Degree = q.Degree,
                    University = q.University,
                    Year = q.Year,
                    Result = q.Result
                }).ToList(),

                Experiences = p.Experiences.Select(e => new PrincipalExperienceDTO
                {
                    Designation = e.Designation,
                    Organization = e.Organization,
                    Place = e.Place,
                    FromDate = e.FromDate.HasValue
                        ? e.FromDate.Value.ToString("yyyy-MM-dd") : null,
                    ToDate = e.ToDate.HasValue
                        ? e.ToDate.Value.ToString("yyyy-MM-dd") : null
                }).ToList(),

                Publications = p.Publications.Select(pub => new PrincipalPublicationDTO
                {
                    Title = pub.Title,
                    JournalOrConference = pub.JournalOrConference,
                    Type = pub.Type,
                    DOI = pub.DOI,
                    Year = pub.Year
                }).ToList(),

                BookPublications = p.BookPublications.Select(b => new PrincipalBookPublicationDTO
                {
                    Title = b.Title,
                    BookCode = b.BookCode,
                    University = b.University,
                    Branch = b.Branch,
                    Semester = b.Semester,
                    ISBN = b.ISBN,
                    Publisher = b.Publisher,
                    ContentTopics = b.ContentTopics
                }).ToList(),

                ExpertTalks = p.ExpertTalks.Select(t => new PrincipalExpertTalkDTO
                {
                    Year = t.Year,
                    Subject = t.Subject,
                    Place = t.Place,
                    Details = t.Details
                }).ToList(),

                Achievements = p.Achievements.Select(a => new PrincipalAchievementDTO
                {
                    AchievementText = a.AchievementText,
                    Year = a.Year
                }).ToList(),

                Memberships = p.Memberships.Select(m => new PrincipalMembershipDTO
                {
                    MembershipText = m.MembershipText
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

            return Ok(ApiResponse<PrincipalProfileDTO>.Ok(data));
        }
    }
}