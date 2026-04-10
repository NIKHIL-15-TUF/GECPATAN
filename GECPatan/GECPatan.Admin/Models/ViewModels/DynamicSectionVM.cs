using GECPatan.Admin.Models.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DynamicSectionVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public SectionType SectionType { get; set; } = SectionType.RichText;

        public string? HtmlContent { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        public PageType PageType { get; set; } = PageType.Department;
        public int PageId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // For dropdown
        public List<SelectListItem> SectionTypes { get; set; } = new();
    }

    public class DynamicSectionListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public SectionType SectionType { get; set; }
        public string SectionTypeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public PageType PageType { get; set; }
        public int PageId { get; set; }
        public string PageName { get; set; } = string.Empty;
    }
}