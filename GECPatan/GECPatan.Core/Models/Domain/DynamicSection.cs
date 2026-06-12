using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    // SECTION TYPE ENUM
    public enum SectionType
    {
        RichText = 1,       // TinyMCE editor
        PDFViewer = 2,      // Inline PDF viewer
        PDFDownload = 3,    // Download button
        ImageGallery = 4,   // Multiple images
        FileList = 5,       // Multiple downloadable files
        Table = 6           // HTML table
    }
    // PAGE TYPE ENUM
    public enum PageType
    {
        Department = 1,
        Committee = 2,
        Facility = 3,
        Custom = 4,
        Principal = 5
    }
    // DYNAMIC SECTION
    public class DynamicSection : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public SectionType SectionType { get; set; } = SectionType.RichText;

        // For RichText and Table
        public string? HtmlContent { get; set; }

        // For single file (PDFViewer, PDFDownload)
        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        // Which page this section belongs to
        public PageType PageType { get; set; } = PageType.Department;
        public int PageId { get; set; }

        // Display
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation - for gallery and file list
        [ValidateNever]
        public ICollection<DynamicSectionFile> Files { get; set; }
            = new List<DynamicSectionFile>();
    }
    // DYNAMIC SECTION FILE
    // (for ImageGallery and FileList types)
    public class DynamicSectionFile : BaseEntity
    {
        public int Id { get; set; }

        public string FilePath { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Title { get; set; }

        // "Image" or "PDF" or "Doc" etc.
        [MaxLength(20)]
        public string FileType { get; set; } = "Image";

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int DynamicSectionId { get; set; }

        [ValidateNever]
        public DynamicSection? DynamicSection { get; set; }
    }
}