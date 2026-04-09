using GECPatan.Admin.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DepartmentCreateVM
    {
        [Required(ErrorMessage = "Department name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Short Code (e.g. CE, IT, ME)")]
        public string? ShortCode { get; set; }

        public string? About { get; set; }

        [Display(Name = "Total Intake")]
        public int Intake { get; set; }

        [Display(Name = "Faculty Count")]
        public int FacultyCount { get; set; }

        [Display(Name = "Labs & Workshops")]
        public int LabCount { get; set; }

        [Display(Name = "Annual Placement")]
        public int AnnualPlacement { get; set; }

        [Display(Name = "HOD Name")]
        public string? HODName { get; set; }

        [Display(Name = "HOD Message")]
        public string? HODMessage { get; set; }

        [Display(Name = "Tagline")]
        public string? Tagline { get; set; }

        [Display(Name = "Show Intake on Website")]
        public bool ShowIntake { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentEditVM : DepartmentCreateVM
    {
        public int DeptId { get; set; }
        public string? ExistingTitleImagePath { get; set; }
        public string? ExistingHODImagePath { get; set; }

        // For managing list items on edit
        public List<string> VisionItems { get; set; } = new();
        public List<string> MissionItems { get; set; } = new();
        public List<string> PEOItems { get; set; } = new();
        public List<string> PSOItems { get; set; } = new();
    }

    public class DepartmentListVM
    {
        public int DeptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public int Intake { get; set; }
        public int FacultyCount { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public string? TitleImagePath { get; set; }
    }
}