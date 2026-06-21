using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── CUTOFF RECORD ───────────────────────────────────────
    public class CutoffRecordVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Academic Year is required")]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "General")]
        public int? GeneralRank { get; set; }

        [Display(Name = "SEBC")]
        public int? SEBCRank { get; set; }

        [Display(Name = "SC")]
        public int? SCRank { get; set; }

        [Display(Name = "ST")]
        public int? STRank { get; set; }

        [Display(Name = "EWS")]
        public int? EWSRank { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
    }

    public class CutoffRecordListVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public List<CutoffRecordRowVM> Records { get; set; } = new();
    }

    public class CutoffRecordRowVM
    {
        public int Id { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public int? GeneralRank { get; set; }
        public int? SEBCRank { get; set; }
        public int? SCRank { get; set; }
        public int? STRank { get; set; }
        public int? EWSRank { get; set; }
        public bool IsVisible { get; set; }
    }

    // ── SCHOLARSHIP RECORD ──────────────────────────────────
    public class ScholarshipRecordVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Scheme name is required")]
        [Display(Name = "Scheme Name")]
        public string SchemeName { get; set; } = string.Empty;

        [Display(Name = "Academic Year")]
        public string? AcademicYear { get; set; }

        [Display(Name = "Total Applications")]
        [Range(0, int.MaxValue)]
        public int TotalApplications { get; set; } = 0;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── INFRASTRUCTURE RECORD ───────────────────────────────
    public class InfrastructureRecordVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Room type is required")]
        [Display(Name = "Room Type")]
        public string RoomType { get; set; } = string.Empty;

        [Display(Name = "Area (Sq. m.)")]
        [Range(0, double.MaxValue)]
        public double AreaSqm { get; set; } = 0;

        [Display(Name = "Building Name")]
        public string? BuildingName { get; set; }

        [Display(Name = "Department (optional)")]
        public int? DeptId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> RoomTypes { get; set; } = new();
    }
}
