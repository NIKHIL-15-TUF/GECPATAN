using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── NEWS ITEM ──
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

        [Display(Name = "Controller Name")]
        public string? ControllerName { get; set; }

        [Display(Name = "Action Name")]
        public string? ActionName { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool ShowInMarquee { get; set; } = false;

        public string? ExistingBannerPath { get; set; }
        public string? ExistingThumbnailPath { get; set; }
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

    // ── NEWS LETTER ───
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

    // ── TENDER ──
    public class TenderVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Upload Date")]
        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }

    // ── IMPORTANT DOCUMENT ──
    public class ImportantDocumentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "File Type")]
        public string FileType { get; set; } = "PDF";

        [Display(Name = "Upload Date")]
        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }

    // ── MOU DOCUMENT ───
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

    // ── ABOUT US ──
    public class AboutUsVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "History text is required")]
        [Display(Name = "History / About Text")]
        public string HistoryText { get; set; } = string.Empty;
    }
}
