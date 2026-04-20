using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── NEWS ITEM ─────────────────────────────────────────
    public class NewsItemVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Display(Name = "Publish Date")]
        [DataType(DataType.Date)]
        public DateTime? PublishDate { get; set; }

        [Display(Name = "External Link")]
        public string? ExternalLink { get; set; }

        // Replaced ControllerName/ActionName with:
        [Display(Name = "Link to Department")]
        public int? LinkDeptId { get; set; }

        [Display(Name = "Link to Committee")]
        public int? LinkCommitteeId { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool ShowInMarquee { get; set; } = false;

        public string? ExistingBannerPath { get; set; }
        public string? ExistingThumbnailPath { get; set; }

        // For dropdowns
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Committees { get; set; } = new();
    }

    public class NewsItemListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PublishDate { get; set; }
        public string? ThumbnailPath { get; set; }
        public bool IsVisible { get; set; }
        public bool ShowInMarquee { get; set; }
        public int ImageCount { get; set; }
        public int FileCount { get; set; }
    }

    // ── NEWS LETTER ───────────────────────────────────────
    public class NewsLetterVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Download Name")]
        public string? DownloadName { get; set; }

        public bool IsVisible { get; set; } = true;
        public string? ExistingFilePath { get; set; }
        public string? ExistingThumbnailPath { get; set; }
    }

    // ── TENDER CATEGORY (parent) ──────────────────────────
    public class TenderCategoryVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<TenderDocumentVM> Documents { get; set; } = new();
    }

    // ── TENDER DOCUMENT (child) ───────────────────────────
    public class TenderDocumentVM
    {
        public int Id { get; set; }

        public int TenderCategoryId { get; set; }

        [Required(ErrorMessage = "Document title is required")]
        [MaxLength(300)]
        public string DocTitle { get; set; } = string.Empty;

        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        public bool IsVisible { get; set; } = true;
        public string? ExistingFilePath { get; set; }
        public string? CategoryTitle { get; set; }
    }

    // ── IMPORTANT DOCUMENT ────────────────────────────────
    public class ImportantDocumentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "File Type")]
        public string FileType { get; set; } = "PDF";

        [Display(Name = "Upload Date")]
        [DataType(DataType.Date)]
        public DateTime? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }

    // ── MOU DOCUMENT ──────────────────────────────────────
    public class MoUDocumentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Month/Year")]
        public string? MonthYear { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }

    // ── ABOUT US ──────────────────────────────────────────
    public class AboutUsVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "History text is required")]
        [Display(Name = "History / About Text")]
        public string HistoryText { get; set; } = string.Empty;
    }
}   