using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    public class Department : BaseEntity
    {
        [Key]
        public int DeptId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ShortCode { get; set; }

        public string? About { get; set; }

        // Stats shown on frontend
        public int Intake { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int AnnualPlacement { get; set; }

        // HOD info
        public string? HODName { get; set; }
        public string? HODMessage { get; set; }
        public string? HODImagePath { get; set; }

        // Display
        public string? TitleImagePath { get; set; }
        public string? Tagline { get; set; }
        public bool ShowIntake { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        [ValidateNever]
        public ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();

        [ValidateNever]
        public ICollection<DepartmentVision> Visions { get; set; } = new List<DepartmentVision>();

        [ValidateNever]
        public ICollection<DepartmentMission> Missions { get; set; } = new List<DepartmentMission>();

        [ValidateNever]
        public ICollection<DepartmentPEO> PEOs { get; set; } = new List<DepartmentPEO>();

        [ValidateNever]
        public ICollection<DepartmentPSO> PSOs { get; set; } = new List<DepartmentPSO>();

        [ValidateNever]
        public ICollection<DepartmentImage> Images { get; set; } = new List<DepartmentImage>();
    }

    public class DepartmentVision : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string VisionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentMission : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string MissionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentPEO : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string PEOText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentPSO : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string PSOText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class DepartmentImage : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}