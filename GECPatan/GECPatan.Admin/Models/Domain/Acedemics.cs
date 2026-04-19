using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Admin.Models.Domain
{
    // ACADEMIC CALENDAR
    public class AcademicCalendar : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? UploadDate { get; set; }
        public int? DeptId { get; set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
    // SSIP DOCUMENT
    public class SSIPDocument : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? UploadDate { get; set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
    // RESEARCH GRANT
    public class ResearchGrant : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? PrincipalInvestigator { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        [MaxLength(100)]
        public string? Duration { get; set; }

        [MaxLength(200)]
        public string? ProjectCost { get; set; }

        [MaxLength(300)]
        public string? SponsoringAuthority { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // PROGRAM INTAKE 
    public class ProgramIntake : BaseEntity
    {
        public int Id { get; set; }

        // Which department
        public int DeptId { get; set; }

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }

        // Which academic year e.g. 2025
        public int IntakeYear { get; set; }

        // Number of seats
        public int Intake { get; set; }

        public bool IsVisible { get; set; } = true;
    }
}
