using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── PRINCIPAL ─────────────────────────────────────────
    public class PrincipalVM
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; }

        [MaxLength(200)]
        public string? Qualification { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        public string? Message { get; set; }
        public string? About { get; set; }
        public string? ExperienceText { get; set; }
        public string? ResearchInterests { get; set; }
        public string? Publications { get; set; }

        public string? ExistingImagePath { get; set; }
    }

    // ── GALLERY IMAGE ─────────────────────────────────────
    public class GalleryImageVM
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Caption { get; set; }

        [MaxLength(200)]
        public string? Category { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ImagePath { get; set; }
    }

    public class GalleryImageListVM
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string? Category { get; set; }
        public string? ImagePath { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── CONTACT ───────────────────────────────────────────
    public class ContactVM
    {
        // ── College info (public-facing, shown on the Contact Us page) ──
        [MaxLength(200)]
        [Display(Name = "College Name")]
        public string? CollegeName { get; set; } = "Government Engineering College, Patan";

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone1 { get; set; }

        [MaxLength(20)]
        public string? Phone2 { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? Email1 { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? Email2 { get; set; }

        [MaxLength(200)]
        [Display(Name = "Office Hours")]
        public string? OfficeHours { get; set; }

        [MaxLength(500)]
        [Display(Name = "Google Maps Embed URL")]
        public string? MapEmbedUrl { get; set; }

        // ── Contact Us form routing (internal — never shown publicly) ──
        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Support Email (receives form submissions)")]
        public string? SupportEmail { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Sender Email (\"From\" address for outgoing mail)")]
        public string? SenderEmail { get; set; }

        // ── Contact Us form copy & categories ──
        [MaxLength(500)]
        [Display(Name = "Success Message")]
        public string? SuccessMessage { get; set; }

        [MaxLength(500)]
        [Display(Name = "Failure Message")]
        public string? FailureMessage { get; set; }

        [MaxLength(500)]
        [Display(Name = "Allowed Categories (comma-separated)")]
        public string? AllowedCategories { get; set; }

        // ── Social media ──
        [MaxLength(500)]
        public string? FacebookUrl { get; set; }

        [MaxLength(500)]
        public string? TwitterUrl { get; set; }

        [MaxLength(500)]
        public string? YoutubeUrl { get; set; }

        [MaxLength(500)]
        public string? LinkedinUrl { get; set; }

        [MaxLength(500)]
        public string? InstagramUrl { get; set; }
    }

    // ── ALUMNI ────────────────────────────────────────────
    public class AlumniVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(10)]
        [Display(Name = "Batch Year")]
        public string? Batch { get; set; }

        [Display(Name = "Testimonial / About")]
        public string? Testimonial { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingImagePath { get; set; }
    }
    // ── SITE SETTING ──────────────────────────────────────
    public class SiteSettingGroupVM
    {
        public string Group { get; set; } = string.Empty;
        public List<SiteSettingItemVM> Items { get; set; } = new();
    }

    public class SiteSettingItemVM
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string Group { get; set; } = string.Empty;
    }
}