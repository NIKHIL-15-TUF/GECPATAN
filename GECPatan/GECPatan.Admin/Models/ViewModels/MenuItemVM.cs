using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // Full tree node — used in Index view
    public class MenuItemTreeVM
    {
        public int Id { get; set; }
        public string MenuText { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string LinkType { get; set; } = "none";
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public int? DynamicId { get; set; }
        public string? DynamicType { get; set; }
        public string? DynamicLabel { get; set; }  // resolved name
        public string? ExternalLink { get; set; }
        public string? CssClass { get; set; }
        public string MenuType { get; set; } = "Main";
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public bool OpenInNewTab { get; set; }
        public int Level { get; set; } = 0;
        public bool IsFirst { get; set; }
        public bool IsLast { get; set; }
        public List<MenuItemTreeVM> Children { get; set; } = new();
    }

    // Create / Edit form
    public class MenuItemFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Menu text is required")]
        [MaxLength(200)]
        [Display(Name = "Menu Text")]
        public string MenuText { get; set; } = string.Empty;

        [Display(Name = "Parent Item")]
        public int? ParentId { get; set; }

        [Required]
        [Display(Name = "Link Type")]
        public string LinkType { get; set; } = "none";

        // Internal
        [MaxLength(100)]
        [Display(Name = "Controller")]
        public string? ControllerName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Action")]
        public string? ActionName { get; set; }

        // Dynamic
        [MaxLength(50)]
        [Display(Name = "Dynamic Type")]
        public string? DynamicType { get; set; }

        [Display(Name = "Select Item")]
        public int? DynamicId { get; set; }

        // External
        [MaxLength(500)]
        [Display(Name = "URL / PDF Path")]
        public string? ExternalLink { get; set; }

        [MaxLength(100)]
        [Display(Name = "CSS Class")]
        public string? CssClass { get; set; }

        [Display(Name = "Menu Section")]
        public string MenuType { get; set; } = "Main";

        [Display(Name = "Position")]
        public int Position { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        [Display(Name = "Open in New Tab")]
        public bool OpenInNewTab { get; set; } = false;

        // Dropdowns
        public List<SelectListItem> ParentOptions { get; set; } = new();
        public List<SelectListItem> DynamicIdOptions { get; set; } = new();
    }
}