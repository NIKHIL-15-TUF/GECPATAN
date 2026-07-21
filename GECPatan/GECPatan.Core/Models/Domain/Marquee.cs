using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    public class Marquee : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        // none / internal / dynamic / external / file
        [MaxLength(20)]
        public string LinkType { get; set; } = "none";

        // Internal / Dynamic routing
        [MaxLength(100)]
        public string? ControllerName { get; set; }

        [MaxLength(100)]
        public string? ActionName { get; set; }

        // Dynamic link
        public int? DynamicId { get; set; }

        [MaxLength(50)]
        public string? DynamicType { get; set; }

        // External URL
        [MaxLength(500)]
        public string? ExternalLink { get; set; }

        // PDF / File upload
        public string? FilePath { get; set; }

        // Validity window
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // NEW: when true, this item shows in the horizontal scrolling
        // ticker directly below the homepage slider. When false (default),
        // it shows in the vertical "UPDATES" list instead alongside any
        // NewsItem flagged with ShowInMarquee.
        public bool HorizontalMarquee { get; set; } = false;
    }
}
