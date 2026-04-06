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
}