using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── ACADEMIC CALENDAR ───
    public class AcademicCalendarVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Upload Date")]
        public string? UploadDate { get; set; }

        [Display(Name = "Department (optional)")]
        public int? DeptId { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }

        public List<SelectListItem> Departments { get; set; } = new();
    }

    public class AcademicCalendarListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? UploadDate { get; set; }
        public string? DeptName { get; set; }
        public bool IsVisible { get; set; }
        public string? FilePath { get; set; }
    }

    // ── SSIP DOCUMENT ────
    public class SSIPDocumentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Upload Date")]
        public string? UploadDate { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }

    // ── RESEARCH GRANT ───
    public class ResearchGrantVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Principal Investigator")]
        [MaxLength(200)]
        public string? PrincipalInvestigator { get; set; }

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [MaxLength(100)]
        public string? Duration { get; set; }

        [Display(Name = "Project Cost")]
        [MaxLength(200)]
        public string? ProjectCost { get; set; }

        [Display(Name = "Sponsoring Authority")]
        [MaxLength(300)]
        public string? SponsoringAuthority { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // ── PROGRAM INTAKE ───
    public class ProgramIntakeIndexVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public List<ProgramIntakeRowVM> Intakes { get; set; } = new();
        public int LatestIntake { get; set; }
        public int LatestYear { get; set; }
    }

    public class ProgramIntakeRowVM
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public int IntakeYear { get; set; }
        public int Intake { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLatest { get; set; }
    }

    public class ProgramIntakeFormVM
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Enter a valid year")]
        [Display(Name = "Academic Year")]
        public int IntakeYear { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "Intake is required")]
        [Range(1, 1000, ErrorMessage = "Intake must be between 1 and 1000")]
        [Display(Name = "Intake (Seats)")]
        public int Intake { get; set; }

        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
    }
}