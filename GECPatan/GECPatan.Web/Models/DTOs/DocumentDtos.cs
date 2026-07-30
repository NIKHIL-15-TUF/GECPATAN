namespace GECPatan.Web.Models.Dtos
{
    // ── DOCUMENT CATEGORIES (for /api/documents/categories) ─
    public class DocumentCategoryListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public string? TitleImagePath { get; set; }
        public bool TableView { get; set; }
        public bool HasYearSections { get; set; }

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

        /// <summary>Populated only when HasYearSections = false.</summary>
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

    public class MoUDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MonthYear { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class SSIPDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? UploadDate { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

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
