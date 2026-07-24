namespace GECPatan.Api.DTOs
{
    // ── DOCUMENT CATEGORIES (for /api/documents/categories) ─
    public class DocumentCategoryListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public int YearCount { get; set; }
        public int FileCount { get; set; }
    }

    // ── DOCUMENT CATEGORY DETAIL (for /api/documents/{categoryId}) ─
    public class DocumentCategoryDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public List<DocumentYearSectionDTO> YearSections { get; set; } = new();
    }

    public class DocumentYearSectionDTO
    {
        public int Id { get; set; }
        public string Year { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<DocumentFileDTO> Files { get; set; } = new();
    }

    public class DocumentFileDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── TENDERS (for /api/documents/tenders) ───────────────
    public class TenderCategoryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<TenderDocumentDTO> Documents { get; set; } = new();
    }

    public class TenderDocumentDTO
    {
        public int Id { get; set; }
        public string DocTitle { get; set; } = string.Empty;
        public string? ValidFrom { get; set; }
        public string? ValidTo { get; set; }
        public string? MonthYear { get; set; }
        public string? FilePath { get; set; }
        public bool IsActive { get; set; }  // within validity window
        public bool IsExpired { get; set; }
    }

    // ── IMPORTANT DOCUMENTS (for /api/documents/important) ─
    public class ImportantDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string? UploadDate { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── MOU DOCUMENTS (for /api/documents/mou) ─────────────
    public class MoUDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MonthYear { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }
}
