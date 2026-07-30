using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DocumentPageVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Existing banner path, shown as a preview on the Edit screen.</summary>
        public string? ExistingBannerPath { get; set; }

        /// <summary>Checked on the Edit screen to explicitly clear the current banner (when no replacement is uploaded).</summary>
        public bool RemoveBanner { get; set; } = false;

        /// <summary>True = Table View, False = List View. Editable at any time.</summary>
        public bool TableView { get; set; } = false;

        /// <summary>
        /// Group documents by year? Only meaningful — and only ever submitted — on the
        /// Create screen. The Edit screen renders this as disabled/read-only and the
        /// controller ignores any posted value for it on Edit.
        /// </summary>
        public bool HasYearSections { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public int YearCount { get; set; }
        public int FileCount { get; set; }
    }

    public class DocumentYearSectionVM
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string? PageTitle { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public int FileCount { get; set; }
    }

    public class DocumentFileVM
    {
        public int Id { get; set; }

        /// <summary>Set when adding/listing a file that belongs to a Year Section.</summary>
        public int? YearSectionId { get; set; }
        public string? YearSectionLabel { get; set; }

        /// <summary>Owning page — always set, whether the file is year-bound or direct.</summary>
        public int PageId { get; set; }
        public string? PageTitle { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }
}