using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
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