using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DepartmentListVM
    {
        public int DeptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? TitleImagePath { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int Intake { get; set; }
    }

    public class DepartmentCreateVM
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ShortCode { get; set; }

        public string? About { get; set; }

        [MaxLength(300)]
        public string? Tagline { get; set; }

        public int AnnualPlacement { get; set; } = 0;
        public bool ShowIntake { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentEditVM : DepartmentCreateVM
    {
        public int DeptId { get; set; }

        // Auto-calculated from DB — READ ONLY
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int Intake { get; set; }

        // Images
        public string? ExistingTitleImagePath { get; set; }
        public List<string> ExistingBannerImages { get; set; } = new();

        // Vision/Mission/PEO/PSO as lists
        public List<string> VisionItems { get; set; } = new();
        public List<string> MissionItems { get; set; } = new();
        public List<string> PEOItems { get; set; } = new();
        public List<string> PSOItems { get; set; } = new();
    }
}