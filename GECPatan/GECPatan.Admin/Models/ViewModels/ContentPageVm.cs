using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class ContentPageListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsVisible { get; set; }
        public string PublicUrl => $"/page/{Slug}";
    }

    public class ContentPageCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        // Auto-generated from title, can be overridden
        [MaxLength(300)]
        [Display(Name = "URL Slug")]
        public string? Slug { get; set; }

        public string? HtmlContent { get; set; }

        public bool IsVisible { get; set; } = true;
    }

    public class ContentPageEditVM : ContentPageCreateVM
    {
        public int Id { get; set; }
        public string GeneratedUrl { get; set; } = string.Empty;
    }

    // ── AUDIT LOG ──────────────────────────────────────────
    public class AuditLogListVM
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Module { get; set; }
        public string? RecordName { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? Details { get; set; }
    }

    public class AuditLogFilterVM
    {
        public string? UserName { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<AuditLogListVM> Logs { get; set; } = new();
        public List<string> Modules { get; set; } = new();
        public List<string> Actions { get; set; } = new();
    }
}