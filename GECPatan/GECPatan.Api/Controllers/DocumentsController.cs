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
        // All visible document categories (list)
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<DocumentCategoryListDTO>>>> GetCategories()
        {
            var categories = await _context.DocumentCategories
                .Include(c => c.YearSections)
                    .ThenInclude(y => y.Files)
                .Where(c => c.IsVisible)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var data = categories.Select(c => new DocumentCategoryListDTO
            {
                Id = c.Id,
                Title = c.Title,
                DisplayOrder = c.DisplayOrder,
                YearCount = c.YearSections.Count,
                FileCount = c.YearSections
                    .SelectMany(y => y.Files)
                    .Count(f => f.IsVisible)
            }).ToList();

            return Ok(ApiResponse<List<DocumentCategoryListDTO>>.Ok(data));
        }

        // GET /api/documents/{categoryId}
        // Year-wise documents in a category
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ApiResponse<DocumentCategoryDetailDTO>>> GetCategoryDetail(int categoryId)
        {
            var category = await _context.DocumentCategories
                .Include(c => c.YearSections.OrderByDescending(y => y.Year))
                    .ThenInclude(y => y.Files.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsVisible);

            if (category == null)
                return NotFound(ApiResponse<DocumentCategoryDetailDTO>.Fail(
                    "Document category not found"));

            var data = new DocumentCategoryDetailDTO
            {
                Id = category.Id,
                Title = category.Title,
                YearSections = category.YearSections
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
                        FilePath = d.FilePath,
                        IsActive = (!d.ValidFrom.HasValue || d.ValidFrom <= now)
                                 && (!d.ValidTo.HasValue || d.ValidTo >= now),
                        IsExpired = d.ValidTo.HasValue && d.ValidTo < now
                    }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<TenderCategoryDTO>>.Ok(data));
        }

        // GET /api/documents/important
        // Important documents list
        [HttpGet("important")]
        public async Task<ActionResult<ApiResponse<List<ImportantDocumentDTO>>>> GetImportant()
        {
            var data = await _context.ImportantDocuments
                .Where(d => d.IsVisible)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new ImportantDocumentDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    FileType = d.FileType,
                    UploadDate = d.UploadDate.HasValue
                        ? d.UploadDate.Value.ToString("yyyy-MM-dd") : null,
                    FilePath = d.FilePath,
                    DisplayOrder = d.DisplayOrder
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ImportantDocumentDTO>>.Ok(data));
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
    }
}