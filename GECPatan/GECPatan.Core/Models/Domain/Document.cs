using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
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

    public class DocumentYearSection : BaseEntity
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public DocumentCategory? Category { get; set; }

        [MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public ICollection<DocumentFile> Files { get; set; }
            = new List<DocumentFile>();
    }

    public class DocumentFile : BaseEntity
    {
        public int Id { get; set; }
        public int YearSectionId { get; set; }

        [ForeignKey("YearSectionId")]
        public DocumentYearSection? YearSection { get; set; }

        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // TENDER — PARENT-CHILD RESTRUCTURE
    // T1 (Parent): TenderCategory — ID | Title
    // T2 (Child):  TenderDocument — DocTitle | DocID | ValidFrom | ValidTo | Path | IsVisible
    public class TenderCategory : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public ICollection<TenderDocument> Documents { get; set; }
            = new List<TenderDocument>();
    }

    public class TenderDocument : BaseEntity
    {
        public int Id { get; set; }

        public int TenderCategoryId { get; set; }

        [ForeignKey("TenderCategoryId")]
        public TenderCategory? TenderCategory { get; set; }

        [Required, MaxLength(300)]
        public string DocTitle { get; set; } = string.Empty;

        // Valid from/to dates
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? MonthYear { get; set; }

        public string? FilePath { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    // IMPORTANT DOCUMENT
    public class ImportantDocument : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(50)]
        public string FileType { get; set; } = "PDF";

        // Changed from string to DateTime?
        public DateTime? UploadDate { get; set; }

        public string? FilePath { get; set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
    // MOU DOCUMENT
    public class MoUDocument : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MonthYear { get; set; }

        public string? FilePath { get; set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
