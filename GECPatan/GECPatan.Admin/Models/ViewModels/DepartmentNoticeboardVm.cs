using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DeptNoticeListVM
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public string? FilePath { get; set; }
        public string? ExternalLink { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? PostedBy { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }

        // Computed
        public bool IsActive =>
            IsVisible
            && (!ValidFrom.HasValue || ValidFrom <= DateTime.Now)
            && (!ValidTo.HasValue || ValidTo >= DateTime.Now);

        public bool IsExpired =>
            ValidTo.HasValue && ValidTo < DateTime.Now;
    }

    public class DeptNoticeFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        [Display(Name = "Notice Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        // File type: PDF / Image / Link / None
        [Display(Name = "Attachment Type")]
        public string FileType { get; set; } = "None";

        public string? ExistingFilePath { get; set; }

        [Display(Name = "External Link")]
        [MaxLength(500)]
        public string? ExternalLink { get; set; }

        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        // For dept dropdown
        public List<SelectListItem> Departments { get; set; } = new();
    }

    // Used for Dept-wise grouped view
    public class DeptNoticeBoardVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Active { get; set; }
        public int Expired { get; set; }
        public List<DeptNoticeListVM> Notices { get; set; } = new();
    }
}