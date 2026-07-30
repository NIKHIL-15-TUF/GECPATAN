namespace GECPatan.Api.DTOs
{
    // ── DOCUMENT CATEGORIES (for /api/documents/categories) ─
    public class DocumentCategoryListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        /// <summary>Web-relative path to the page's title banner image, or null if none set.</summary>
        public string? TitleImagePath { get; set; }

        /// <summary>True = the page renders as a Table View, false = List/Card View.</summary>
        public bool TableView { get; set; }

        /// <summary>True = documents are grouped under Year Sections; false = uploaded directly under the page.</summary>
        public bool HasYearSections { get; set; }

        /// <summary>Only meaningful when HasYearSections = true.</summary>
        public int YearCount { get; set; }
        public int FileCount { get; set; }
    }

    // ── DOCUMENT CATEGORY DETAIL (for /api/documents/{categoryId}) ─
    public class DocumentCategoryDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public string? TitleImagePath { get; set; }
        public bool TableView { get; set; }
        public bool HasYearSections { get; set; }

        /// <summary>Populated only when HasYearSections = true.</summary>
        public List<DocumentYearSectionDTO> YearSections { get; set; } = new();

        /// <summary>Populated only when HasYearSections = false (files uploaded directly under the page).</summary>
        public List<DocumentFileDTO> DirectFiles { get; set; } = new();
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

    // ── MOU DOCUMENTS (for /api/documents/mou) ─────────────
    public class MoUDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MonthYear { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── SSIP DOCUMENTS (for /api/documents/ssip) ───────────
    public class SSIPDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? UploadDate { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── TIMETABLES (for /api/documents/timetable) ──────────
    public class TimetableDTO
    {
        public int Id { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Semester { get; set; }
        public string SemesterType { get; set; } = string.Empty;
        public string? FilePath { get; set; }
    }
}
