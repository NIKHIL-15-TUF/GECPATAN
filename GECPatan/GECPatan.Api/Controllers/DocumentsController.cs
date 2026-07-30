using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentsController(ApplicationDbContext context)
            => _context = context;

        // GET /api/documents/categories
        // All visible document pages (list) — the response is still called
        // "categories" for URL/API-contract stability, even though the admin
        // side now calls these "Document Pages".
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<DocumentCategoryListDTO>>>> GetCategories()
        {
            var pages = await _context.DocumentPages
                .Include(p => p.YearSections)
                    .ThenInclude(y => y.Files)
                .Include(p => p.Files)
                .Where(p => p.IsVisible)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            var data = pages.Select(p => new DocumentCategoryListDTO
            {
                Id = p.Id,
                Title = p.Title,
                DisplayOrder = p.DisplayOrder,
                TitleImagePath = p.TitleBannerImagePath,
                TableView = p.TableView,
                HasYearSections = p.HasYearSections,
                YearCount = p.YearSections.Count,
                FileCount = p.HasYearSections
                    ? p.YearSections.SelectMany(y => y.Files).Count(f => f.IsVisible)
                    : p.Files.Count(f => f.IsVisible)
            }).ToList();

            return Ok(ApiResponse<List<DocumentCategoryListDTO>>.Ok(data));
        }

        // GET /api/documents/{categoryId}
        // Year-wise documents (when HasYearSections = true) or direct files
        // (when HasYearSections = false) for a single document page.
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ApiResponse<DocumentCategoryDetailDTO>>> GetCategoryDetail(int categoryId)
        {
            var page = await _context.DocumentPages
                .Include(p => p.YearSections.OrderByDescending(y => y.Year))
                    .ThenInclude(y => y.Files.OrderBy(f => f.DisplayOrder))
                .Include(p => p.Files.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == categoryId && p.IsVisible);

            if (page == null)
                return NotFound(ApiResponse<DocumentCategoryDetailDTO>.Fail(
                    "Document category not found"));

            var data = new DocumentCategoryDetailDTO
            {
                Id = page.Id,
                Title = page.Title,
                TitleImagePath = page.TitleBannerImagePath,
                TableView = page.TableView,
                HasYearSections = page.HasYearSections,

                YearSections = page.HasYearSections
                    ? page.YearSections
                        .OrderByDescending(y => y.Year)
                        .ThenBy(y => y.DisplayOrder)
                        .Select(y => new DocumentYearSectionDTO
                        {
                            Id = y.Id,
                            Year = y.Year,
                            DisplayOrder = y.DisplayOrder,
                            Files = y.Files
                                .Where(f => f.IsVisible)
                                .OrderBy(f => f.DisplayOrder)
                                .Select(f => new DocumentFileDTO
                                {
                                    Id = f.Id,
                                    Title = f.Title,
                                    FilePath = f.FilePath,
                                    DisplayOrder = f.DisplayOrder
                                }).ToList()
                        }).ToList()
                    : new List<DocumentYearSectionDTO>(),

                DirectFiles = !page.HasYearSections
                    ? page.Files
                        .Where(f => f.IsVisible)
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => new DocumentFileDTO
                        {
                            Id = f.Id,
                            Title = f.Title,
                            FilePath = f.FilePath,
                            DisplayOrder = f.DisplayOrder
                        }).ToList()
                    : new List<DocumentFileDTO>()
            };

            return Ok(ApiResponse<DocumentCategoryDetailDTO>.Ok(data));
        }

        // GET /api/documents/tenders
        // All tender categories + documents
        [HttpGet("tenders")]
        public async Task<ActionResult<ApiResponse<List<TenderCategoryDTO>>>> GetTenders()
        {
            var now = DateTime.Now;

            var categories = await _context.TenderCategories
                .Include(c => c.Documents)
                .Where(c => c.IsVisible)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var data = categories.Select(c => new TenderCategoryDTO
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                Documents = c.Documents
                    .Where(d => d.IsVisible)
                    .OrderByDescending(d => d.ValidFrom)
                    .Select(d => new TenderDocumentDTO
                    {
                        Id = d.Id,
                        DocTitle = d.DocTitle,
                        ValidFrom = d.ValidFrom.HasValue
                            ? d.ValidFrom.Value.ToString("yyyy-MM-dd") : null,
                        ValidTo = d.ValidTo.HasValue
                            ? d.ValidTo.Value.ToString("yyyy-MM-dd") : null,
                        MonthYear = d.MonthYear,
                        FilePath = d.FilePath,
                        IsActive = (!d.ValidFrom.HasValue || d.ValidFrom <= now)
                                 && (!d.ValidTo.HasValue || d.ValidTo >= now),
                        IsExpired = d.ValidTo.HasValue && d.ValidTo < now
                    }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<TenderCategoryDTO>>.Ok(data));
        }

        // GET /api/documents/mou
        // MoU documents list
        [HttpGet("mou")]
        public async Task<ActionResult<ApiResponse<List<MoUDocumentDTO>>>> GetMoUs()
        {
            var data = await _context.MoUDocuments
                .Where(d => d.IsVisible)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new MoUDocumentDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    MonthYear = d.MonthYear,
                    FilePath = d.FilePath,
                    DisplayOrder = d.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MoUDocumentDTO>>.Ok(data));
        }

        // GET /api/documents/ssip
        [HttpGet("ssip")]
        public async Task<ActionResult<ApiResponse<List<SSIPDocumentDTO>>>> GetSSIP()
        {
            var data = await _context.SSIPDocuments
                .Where(d => d.IsVisible)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SSIPDocumentDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    UploadDate = d.UploadDate,
                    FilePath = d.FilePath,
                    DisplayOrder = d.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<SSIPDocumentDTO>>.Ok(data));
        }

        // GET /api/documents/timetable
        // Only the current/latest timetable per department+semester is public-facing;
        // older uploads are retained as history (see Timetable.IsLatest in Admin).
        [HttpGet("timetable")]
        public async Task<ActionResult<ApiResponse<List<TimetableDTO>>>> GetTimetables()
        {
            var data = await _context.Timetables
                .Include(t => t.Department)
                .Where(t => t.IsVisible && t.IsLatest)
                .OrderBy(t => t.Department!.Name).ThenBy(t => t.Semester)
                .Select(t => new TimetableDTO
                {
                    Id = t.Id,
                    DeptName = t.Department != null ? t.Department.Name : "",
                    Year = t.Year,
                    Semester = t.Semester,
                    SemesterType = t.SemesterType == 1 ? "Odd" : "Even",
                    FilePath = t.FilePath
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TimetableDTO>>.Ok(data));
        }
    }
}
