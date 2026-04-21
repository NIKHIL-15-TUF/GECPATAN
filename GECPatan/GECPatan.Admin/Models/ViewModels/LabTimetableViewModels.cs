using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── LAB ───────────────────────────────────────────────
    public class LabListVM
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public string? DeptName { get; set; }
        public int ImageCount { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class LabCreateVM
    {
        [Required(ErrorMessage = "Lab name is required")]
        [MaxLength(200)]
        public string LabName { get; set; } = string.Empty;

        // TinyMCE RTE
        public string? About { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
    }

    public class LabEditVM : LabCreateVM
    {
        public int LabId { get; set; }
        public List<string> ExistingImagePaths { get; set; } = new();
    }

    // ── TIMETABLE ─────────────────────────────────────────
    public class TimetableListVM
    {
        public int Id { get; set; }
        public string? DeptName { get; set; }
        public int DeptId { get; set; }
        public int Year { get; set; }
        public int Semester { get; set; }
        public int SemesterType { get; set; } // 1=Odd, 2=Even
        public string? FilePath { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLatest { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class TimetableCreateVM
    {
        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        public int Year { get; set; } = DateTime.Today.Year;

        [Display(Name = "Semester Type")]
        public int SemesterType { get; set; } = 1; // 1=Odd, 2=Even

        [Required(ErrorMessage = "Semester is required")]
        public int Semester { get; set; }

        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
    }
}