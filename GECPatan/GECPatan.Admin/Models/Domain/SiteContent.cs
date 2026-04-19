using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    // ALUMNI
    public class Alumni : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Batch { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [MaxLength(200)]
        public string? Designation { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public string? ImagePath { get; set; }
        public string? Testimonial { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
    // SETTINGS
    // Simple key-value store for site-wide settings
    public class SiteSetting : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        // Group: "General", "Contact", "Social", "SEO"
        [MaxLength(50)]
        public string Group { get; set; } = "General";
    }
    // ABOUT US
    public class AboutUs : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string HistoryText { get; set; } = string.Empty;

        // Images stored as comma-separated paths
        public string? ImagePaths { get; set; }
    }
    // GALLERY IMAGE
    public class GalleryImage : BaseEntity
    {
        public int Id { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // CONTACT INFO
    public class ContactInfo : BaseEntity
    {
        public int Id { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? MapEmbedUrl { get; set; }
    }
    // CONTENT PAGE (standalone TinyMCE pages)
    public class ContentPage : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        // Auto generated from title e.g. nirf-report-2025
        [MaxLength(300)]
        public string Slug { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }

        [MaxLength(200)]
        public string? CreatedBy { get; set; }

        public bool IsVisible { get; set; } = true;
    }
    // AUDIT LOG
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? UserId { get; set; }

        [MaxLength(200)]
        public string? UserName { get; set; }

        [MaxLength(100)]
        public string? UserRole { get; set; }

        // Created / Updated / Deleted / Login
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        // Faculty / Department / NewsItem etc.
        [MaxLength(100)]
        public string? Module { get; set; }

        public int? RecordId { get; set; }

        [MaxLength(300)]
        public string? RecordName { get; set; }

        // JSON string of changes
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}
