using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    // DOCUMENT PAGE
    // e.g. "MoU", "NAAC", "Syllabus", "AICTE Approvals"
    //
    // A page can either group its files under Year Sections, or accept
    // files uploaded directly against the page. Which mode a page uses is
    // decided once, at creation time (HasYearSections), and is never
    // editable afterwards — see ImportantDocumentController for the
    // enforcement of that rule.
    public class DocumentPage : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Web-relative path of the optional page header banner image.</summary>
        public string? TitleBannerImagePath { get; set; }

        /// <summary>
        /// True = render the document list as a Table View, False = simple List View.
        /// Editable at any time.
        /// </summary>
        public bool TableView { get; set; } = false;

        /// <summary>
        /// True = documents are organized under Year Sections (Page → Year → Files).
        /// False = documents are uploaded directly under the page (Page → Files).
        /// LOCKED FOREVER once the page is created — controllers must never allow
        /// this to be changed via the Edit screen.
        /// </summary>
        public bool HasYearSections { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation

        [ValidateNever]
        public ICollection<DocumentYearSection> YearSections { get; set; }
            = new List<DocumentYearSection>();

        /// <summary>Files uploaded directly to this page (only populated when HasYearSections = false).</summary>
        [ValidateNever]
        public ICollection<DocumentFile> Files { get; set; }
            = new List<DocumentFile>();
    }

    public class DocumentYearSection : BaseEntity
    {
        public int Id { get; set; }
        public int PageId { get; set; }

        [ForeignKey("PageId")]
        public DocumentPage? Page { get; set; }

        [MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public ICollection<DocumentFile> Files { get; set; }
            = new List<DocumentFile>();
    }

    public class DocumentFile : BaseEntity
    {
        public int Id { get; set; }

        /// <summary>Set when this file belongs to a Year Section (year-grouped pages).</summary>
        public int? YearSectionId { get; set; }

        [ForeignKey("YearSectionId")]
        public DocumentYearSection? YearSection { get; set; }

        /// <summary>Set when this file is uploaded directly under a page (non-year-grouped pages).</summary>
        public int? PageId { get; set; }

        [ForeignKey("PageId")]
        public DocumentPage? Page { get; set; }

        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // TENDER — PARENT-CHILD RESTRUCTURE  (untouched — separate module)
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

    // MOU DOCUMENT  (untouched — separate module)
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