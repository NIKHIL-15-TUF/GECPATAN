using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    // SLIDER
    public class Slider : BaseEntity
    {
        public int Id { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(300)]
        public string? H3Text { get; set; }

        [MaxLength(300)]
        public string? H4Text { get; set; }

        [MaxLength(300)]
        public string? H5Text { get; set; }

        [MaxLength(200)]
        public string? Anchor1Text { get; set; }

        [MaxLength(500)]
        public string? Anchor1Link { get; set; }

        [MaxLength(200)]
        public string? Anchor2Text { get; set; }

        [MaxLength(500)]
        public string? Anchor2Link { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }
    // MARQUEE
    // Type: 1 = Update, 2 = Activity
    public class Marquee : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        // 1 = Update, 2 = Activity
        public int MarqueeType { get; set; } = 1;

        // File link (PDF etc.)
        public string? FileLink { get; set; }

        // Or page link (controller + action)
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public int? DynamicId { get; set; }

        public bool IsFile { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // TESTIMONIAL
    public class Testimonial : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string StudentName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? PassoutYear { get; set; }

        [Required]
        public string TestimonialText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }
    // TOP RECRUITER
    public class TopRecruiter : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? LogoPath { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }
}