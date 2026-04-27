using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── LIST ─────────────────────────────────────────────
    public class PrincipalListVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? PhotoPath { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DateOfJoiningInstitute { get; set; }
        public DateTime? TransferDate { get; set; }
        public string? TransferNote { get; set; }
    }

    // ── MAIN FORM ─────────────────────────────────────────
    public class PrincipalFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; } = "Principal";

        [MaxLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        public string? ExistingPhotoPath { get; set; }

        public string? Message { get; set; }

        [MaxLength(500)]
        [Display(Name = "Area of Interest")]
        public string? AreaOfInterest { get; set; }

        [Display(Name = "Date of Joining (Institute)")]
        [DataType(DataType.Date)]
        public DateTime? DateOfJoiningInstitute { get; set; }

        [Display(Name = "Date of Joining (Department)")]
        [DataType(DataType.Date)]
        public DateTime? DateOfJoiningDept { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ── SUB SECTION VMs ───────────────────────────────────

    public class PrincipalQualificationVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Degree is required")]
        [MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? University { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [MaxLength(100)]
        public string? Result { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalExperienceVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Organization { get; set; }

        [MaxLength(200)]
        public string? Place { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalPublicationVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? JournalOrConference { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "Journal";

        [MaxLength(200)]
        public string? DOI { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalBookPublicationVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? BookCode { get; set; }

        [MaxLength(200)]
        public string? University { get; set; }

        [MaxLength(100)]
        public string? Branch { get; set; }

        [MaxLength(50)]
        public string? Semester { get; set; }

        [MaxLength(50)]
        public string? ISBN { get; set; }

        [MaxLength(200)]
        public string? Publisher { get; set; }

        public string? ContentTopics { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalExpertTalkVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Place { get; set; }

        public string? Details { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalAchievementVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Achievement is required")]
        public string AchievementText { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PrincipalMembershipVM
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [Required(ErrorMessage = "Membership is required")]
        public string MembershipText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }

    // ── TRANSFER VM ───────────────────────────────────────
    public class PrincipalTransferVM
    {
        public int CurrentPrincipalId { get; set; }
        public string CurrentPrincipalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transfer date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Transfer Date")]
        public DateTime TransferDate { get; set; } = DateTime.Today;

        [MaxLength(300)]
        [Display(Name = "Transfer Note")]
        public string? TransferNote { get; set; }
    }
}