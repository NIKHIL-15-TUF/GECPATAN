using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    // DOCUMENT CATEGORY
    // (e.g. "MoU", "NAAC", "Syllabus")
    public class DocumentCategory : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation
        [ValidateNever]
        public ICollection<DocumentYearSection> YearSections { get; set; }
            = new List<DocumentYearSection>();
    }

    // DOCUMENT YEAR SECTION
    // (e.g. "2024-25", "2023-24")
    public class DocumentYearSection : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // FK
        public int DocumentCategoryId { get; set; }

        [ValidateNever]
        public DocumentCategory? DocumentCategory { get; set; }

        // Navigation
        [ValidateNever]
        public ICollection<DocumentFile> Files { get; set; }
            = new List<DocumentFile>();
    }

    // DOCUMENT FILE
    public class DocumentFile : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        [MaxLength(20)]
        public string FileType { get; set; } = "PDF";

        public string? MonthYear { get; set; }
        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        // FK
        public int DocumentYearSectionId { get; set; }

        [ValidateNever]
        public DocumentYearSection? DocumentYearSection { get; set; }
    }
    // TENDER
    public class Tender : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }
        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
    // IMPORTANT DOCUMENT
    public class ImportantDocument : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        [MaxLength(20)]
        public string FileType { get; set; } = "PDF";

        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // MOU DOCUMENT
    public class MoUDocument : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }
        public string? MonthYear { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}