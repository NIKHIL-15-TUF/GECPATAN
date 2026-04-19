using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Admin.Models.Domain
{
    public class Department : BaseEntity
    {
        [Key]
        public int? DeptId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ShortCode { get; set; }

        public string? About { get; set; }
        public string? TitleImagePath { get; set; }

        [MaxLength(300)]
        public string? Tagline { get; set; }

        // Intake is managed via ProgramIntake table now, FacultyCount is auto-fetched from Faculty table, LabCount is auto-fetched from Lab table

        public bool ShowIntake { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Annual placement stat for display
        public int AnnualPlacement { get; set; } = 0;

        // Navigation
        public ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
        public ICollection<DepartmentBannerImage> BannerImages { get; set; } = new List<DepartmentBannerImage>();
        public ICollection<DepartmentVision> Visions { get; set; } = new List<DepartmentVision>();
        public ICollection<DepartmentMission> Missions { get; set; } = new List<DepartmentMission>();
        public ICollection<DepartmentPEO> PEOs { get; set; } = new List<DepartmentPEO>();
        public ICollection<DepartmentPSO> PSOs { get; set; } = new List<DepartmentPSO>();
        public ICollection<Lab> Labs { get; set; } = new List<Lab>();
    
    }


    // Banner Images (multiple, carousel)
    public class DepartmentBannerImage : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }
    }

    public class DepartmentVision : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string VisionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }
    }

    public class DepartmentMission : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string MissionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }
    }

    public class DepartmentPEO : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string PEOText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }
    }
    public class DepartmentPSO : BaseEntity
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string PSOText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }
    }
        public class Timetable : BaseEntity
        {
            public int Id { get; set; }

            public int DeptId { get; set; }

            [ForeignKey("DeptId")]
            public Department? Department { get; set; }

            public int Year { get; set; }

            // 1=Odd, 2=Even
            public int SemesterType { get; set; } = 1;

            // 1,2,3,4,5,6,7,8
            public int Semester { get; set; }

            public string? FilePath { get; set; }

            [MaxLength(200)]
            public string? UploadedBy { get; set; }

            public DateTime UploadedDate { get; set; } = DateTime.Now;

            // Only latest shown on frontend
            // Old files kept as history
            public bool IsLatest { get; set; } = true;
            public bool IsVisible { get; set; } = true;
        }
    public class Lab : BaseEntity
    {
        public int LabId { get; set; }

        public int DeptId { get; set; }

        [ForeignKey("DeptId")]
        [ValidateNever]
        public Department? Department { get; set; }

        [Required, MaxLength(200)]
        public string LabName { get; set; } = string.Empty;

        // TinyMCE RTE content
        public string? About { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation
        [ValidateNever]
        public ICollection<LabImage> Images { get; set; }
            = new List<LabImage>();
    }

    // =============================================
    // LAB IMAGE (carousel)
    // =============================================
    public class LabImage : BaseEntity
    {
        public int LabImageId { get; set; }

        public int LabId { get; set; }

        [ForeignKey("LabId")]
        [ValidateNever]
        public Lab? Lab { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }
}
