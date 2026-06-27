using  GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace GECPatan.Admin.Models.ViewModels
{
    public class FacultyListVM
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public bool IsTeaching { get; set; }
        public int SeniorityOrder { get; set; }
        public DateTime DateOfJoining { get; set; }
    }

    public class FacultyCreateVM
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Designation is required")]
        [MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Date of Joining is required")]
        [Display(Name = "Date of Joining")]
        [DataType(DataType.Date)]
        public DateTime DateOfJoining { get; set; } = DateTime.Today;

        // Qualification REMOVED from here
        // Use FacultyQualification sub-section instead

        [Display(Name = "Area of Interest")]
        public string? AreaOfInterest { get; set; }

        public string? Website { get; set; }

        // IsTeaching auto-set from Designation — not editable
        public bool IsTeaching { get; set; } = true;

        [Display(Name = "Seniority Order")]
        public int SeniorityOrder { get; set; } = 0;

        // Dropdowns
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Designations { get; set; } = new();
    }

    public class FacultyEditVM : FacultyCreateVM
    {
        public int FacultyId { get; set; }
        public string? ExistingImagePath { get; set; }
    }

    // ── SUB SECTION VMs ──────────────────────────────

    public class QualificationVM
    {
        public int FacultyQualificationId { get; set; }

        // Dropdown: PhD / Pursuing PhD / ME/MTech / BE/BTech
        [Required(ErrorMessage = "Degree is required")]
        [MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? University { get; set; }

        [MaxLength(20)]
        public string? Year { get; set; }

        public string? Specialization { get; set; }
        public int FacultyId { get; set; }

        public List<SelectListItem> DegreeOptions { get; set; } = new();
    }

    public class ExperienceVM
    {
        public int FacultyExperienceId { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organization is required")]
        [MaxLength(300)]
        public string Organization { get; set; } = string.Empty;

        // Changed: Duration → FromDate + ToDate
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }
        // ToDate null = Present

        public int FacultyId { get; set; }
    }

    public class TrainingVM
    {
        public int FacultyTrainingId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? OrganizedBy { get; set; }

        // Changed: Date → FromDate + ToDate
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }
        public string? TrainingType { get; set; }  
        public int FacultyId { get; set; }
    }

    public class PublicationVM
    {
        public int FacultyPublicationId { get; set; }
        public int SrNo { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "International";
        public string? JournalName { get; set; }
        public string? Year { get; set; }
        public string? CoAuthors { get; set; }
        public int FacultyId { get; set; }
    }

    public class PersonalDetailVM
    {
        public int PersonalDetailId { get; set; }
        public string? Department { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }  
        public int FacultyId { get; set; }
    }

    public class FacultySubjectVM
    {
        public int FacultySubjectId { get; set; }
        public int FacultyId { get; set; }

        [Required]
        [Display(Name = "Subject Name")]
        public string SubjectName { get; set; } = string.Empty;

        [Display(Name = "Level")]
        public string Level { get; set; } = "UG";

        public int DisplayOrder { get; set; } = 0;
    }
    public class FacultyResearchGuidanceVM
    {
        public int FacultyResearchGuidanceId { get; set; }
        public int FacultyId { get; set; }

        [Display(Name = "Level")]
        public string Level { get; set; } = "Masters";

        [Display(Name = "Ongoing")]
        public int Ongoing { get; set; } = 0;

        [Display(Name = "Completed")]
        public int Completed { get; set; } = 0;
    }
    public class FacultyBookPublicationVM
    {
        public int FacultyBookPublicationId { get; set; }
        public int FacultyId { get; set; }

        public int SrNo { get; set; } = 1;

        [Required]
        [Display(Name = "Book Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Publisher")]
        public string? Publisher { get; set; }

        [Display(Name = "Year")]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }
    public class FacultyConsultancyVM
    {
        public int Id { get; set; }

        public int FacultyId { get; set; }

        [Required(ErrorMessage = "Project title is required")]
        [Display(Name = "Project / Consultancy Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Client / Organization")]
        public string? Client { get; set; }

        [Display(Name = "Year")]
        [MaxLength(10)]
        public string? Year { get; set; }

        [Display(Name = "Amount (if any)")]
        [MaxLength(100)]
        public string? Amount { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class FacultyPatentVM
    {
        public int FacultyPatentId { get; set; }
        public int FacultyId { get; set; }

        [Required]
        [Display(Name = "Patent Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Application No")]
        public string? ApplicationNo { get; set; }

        [Display(Name = "Granted Year")]
        public string? GrantedYear { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Filed";

        public int DisplayOrder { get; set; } = 0;
    }
    public class FacultyProfessionalMembershipVM
    {
        public int FacultyProfessionalMembershipId { get; set; }
        public int FacultyId { get; set; }

        [Required]
        [Display(Name = "Community")]
        public string Community { get; set; } = string.Empty;

        [Display(Name = "Membership Type")]
        public string MembershipType { get; set; } = "Member";

        public int DisplayOrder { get; set; } = 0;
    }
}