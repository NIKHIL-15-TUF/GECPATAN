using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Admin.Models.Domain
{
    public class MenuItem : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string MenuText { get; set; } = string.Empty;

        // null = Level 1 (top navbar)
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public MenuItem? Parent { get; set; }

        public ICollection<MenuItem> Children { get; set; }
            = new List<MenuItem>();

        // ── LINK TYPE ─────────────────────────────────────
        // "none"     = container (has children, no link)
        // "internal" = controller + action
        // "dynamic"  = controller + action + dynamicId
        // "external" = direct URL or PDF path
        [MaxLength(20)]
        public string LinkType { get; set; } = "none";

        // Internal / Dynamic routing
        [MaxLength(100)]
        public string? ControllerName { get; set; }

        [MaxLength(100)]
        public string? ActionName { get; set; }

        // Dynamic ID — links to Dept/Committee/Facility/Doc etc.
        public int? DynamicId { get; set; }

        // What DynamicId refers to
        // Department / Committee / Facility / Club / Document / ContentPage
        [MaxLength(50)]
        public string? DynamicType { get; set; }

        // External URL or PDF path
        [MaxLength(500)]
        public string? ExternalLink { get; set; }

        // Display
        [MaxLength(100)]
        public string? CssClass { get; set; }

        // "Main" = top navbar | "Footer" = footer section
        [MaxLength(20)]
        public string MenuType { get; set; } = "Main";

        public int Position { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public bool OpenInNewTab { get; set; } = false;
    }
}