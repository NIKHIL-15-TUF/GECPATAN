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
        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone1 { get; set; }

        [MaxLength(20)]
        public string? Phone2 { get; set; }

        [MaxLength(200)]
        public string? Email1 { get; set; }

        [MaxLength(200)]
        public string? Email2 { get; set; }

        [MaxLength(500)]
        public string? MapEmbedUrl { get; set; }

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