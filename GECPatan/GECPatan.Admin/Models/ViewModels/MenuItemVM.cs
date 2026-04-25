using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
 
namespace GECPatan.Admin.Models.ViewModels
{
    // Used in tree view
    public class MenuItemTreeVM
    {
        public int Id { get; set; }
        public string MenuText { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public int? DynamicId { get; set; }
        public string? DynamicType { get; set; }
        public string? Link { get; set; }
        public string? CssClass { get; set; }
        public string MenuType { get; set; } = "Main";
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public bool OpenInNewTab { get; set; }
        public int Level { get; set; } = 0;
        public List<MenuItemTreeVM> Children { get; set; } = new();
    }

    // Used in Create/Edit form
    public class MenuItemFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Menu text is required")]
        [MaxLength(200)]
        [Display(Name = "Menu Text")]
        public string MenuText { get; set; } = string.Empty;

        [Display(Name = "Parent Menu")]
        public int? ParentId { get; set; }

        // Link type selection
        // "internal" = controller/action
        // "dynamic"  = controller/action + dynamicId
        // "external" = direct URL or PDF
        // "none"     = just a parent container (no link)
        [Required]
        [Display(Name = "Link Type")]
        public string LinkType { get; set; } = "none";

        [Display(Name = "Controller")]
        [MaxLength(100)]
        public string? ControllerName { get; set; }

        [Display(Name = "Action")]
        [MaxLength(100)]
        public string? ActionName { get; set; }

        [Display(Name = "Dynamic Type")]
        [MaxLength(50)]
        public string? DynamicType { get; set; }

        [Display(Name = "Dynamic ID")]
        public int? DynamicId { get; set; }

        [Display(Name = "URL / PDF Path")]
        [MaxLength(500)]
        public string? Link { get; set; }

        [Display(Name = "CSS Class")]
        [MaxLength(100)]
        public string? CssClass { get; set; }

        [Display(Name = "Menu Section")]
        public string MenuType { get; set; } = "Main";

        [Display(Name = "Position")]
        public int Position { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        [Display(Name = "Open in New Tab")]
        public bool OpenInNewTab { get; set; } = false;

        // Dropdowns
        public List<SelectListItem> ParentItems { get; set; } = new();
        public List<SelectListItem> DepartmentItems { get; set; } = new();
        public List<SelectListItem> CommitteeItems { get; set; } = new();
        public List<SelectListItem> FacilityItems { get; set; } = new();
        public List<SelectListItem> ClubItems { get; set; } = new();
        public List<SelectListItem> ContentPageItems { get; set; } = new();
        public List<SelectListItem> DocumentCatItems { get; set; } = new();
    }
}