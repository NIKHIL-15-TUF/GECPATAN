using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── SLIDER ───
    public class SliderVM
    {
        public int Id { get; set; }

        [Display(Name = "Heading (H3)")]
        public string? H3Text { get; set; }

        [Display(Name = "Sub Heading (H4)")]
        public string? H4Text { get; set; }

        [Display(Name = "Small Text (H5)")]
        public string? H5Text { get; set; }

        [Display(Name = "Button 1 Text")]
        public string? Anchor1Text { get; set; }

        [Display(Name = "Button 1 Link")]
        public string? Anchor1Link { get; set; }

        [Display(Name = "Button 2 Text")]
        public string? Anchor2Text { get; set; }

        [Display(Name = "Button 2 Link")]
        public string? Anchor2Link { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ExistingImagePath { get; set; }
    }

    // ── MARQUEE ──
    public class MarqueeVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Type")]
        public int MarqueeType { get; set; } = 1;

        [Display(Name = "File Link (PDF URL)")]
        public string? FileLink { get; set; }

        [Display(Name = "Controller Name")]
        public string? ControllerName { get; set; }

        [Display(Name = "Action Name")]
        public string? ActionName { get; set; }

        [Display(Name = "Dynamic ID")]
        public int? DynamicId { get; set; }

        public bool IsFile { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── TESTIMONIAL ────
    public class TestimonialVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student name is required")]
        [MaxLength(200)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(20)]
        [Display(Name = "Passout Year")]
        public string? PassoutYear { get; set; }

        [Required(ErrorMessage = "Testimonial text is required")]
        [Display(Name = "Testimonial")]
        public string TestimonialText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── TOP RECRUITER ──
    public class TopRecruiterVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ExistingLogoPath { get; set; }
    }
}
