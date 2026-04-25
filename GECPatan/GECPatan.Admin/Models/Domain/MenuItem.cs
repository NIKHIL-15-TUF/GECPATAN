using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Admin.Models.Domain
{
    public class MenuItem : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string MenuText { get; set; } = string.Empty;

        // null = top level (Level 1)
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public MenuItem? Parent { get; set; }

        public ICollection<MenuItem> Children { get; set; }
            = new List<MenuItem>();

        // Routing — internal
        [MaxLength(100)]
        public string? ControllerName { get; set; }

        [MaxLength(100)]
        public string? ActionName { get; set; }

        // Dynamic ID — links to dept/committee/facility/club etc.
        public int? DynamicId { get; set; }

        // What DynamicId refers to
        // "Department" / "Committee" / "Facility" / "Club" / "Document" / "ContentPage"
        [MaxLength(50)]
        public string? DynamicType { get; set; }

        // External link or PDF path
        [MaxLength(500)]
        public string? Link { get; set; }

        // CSS class (e.g. "MenuOrange" for NBA menu)
        [MaxLength(100)]
        public string? CssClass { get; set; }

        // "Main" = top navbar, "Footer" = footer links
        [MaxLength(20)]
        public string MenuType { get; set; } = "Main";

        public int Position { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Open in new tab?
        public bool OpenInNewTab { get; set; } = false;
    }
}
