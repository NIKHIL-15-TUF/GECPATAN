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
        [MaxLength(50)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Completion Date")]
        [MaxLength(50)]
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
    public class ProgramIntakeVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Program name is required")]
        [MaxLength(200)]
        [Display(Name = "Program Name")]
        public string ProgramName { get; set; } = string.Empty;

        [Required]
        public int Intake { get; set; }

        [Display(Name = "Course Code")]
        [MaxLength(20)]
        public string? CourseCode { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }
}