using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    public class Faculty : BaseEntity
    {
        public int FacultyId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Values: Professor / Associate Professor /
        // Assistant Professor / Lab Assistant /
        // Technical Assistant / Clerk / Other
        [MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        public int DeptId { get; set; }

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }

        public DateTime DateOfJoining { get; set; } = DateTime.Today;
        public string? LetterNumber { get; set; }

        // Qualification removed — use FacultyQualification table
        public string? AreaOfInterest { get; set; }
        public string? Website { get; set; }
        public string? ImagePath { get; set; }

        // Auto-set based on Designation
        // Teaching: Professor/AssocProf/AsstProf
        // Non-Teaching: Lab Assistant/Technical/Clerk/Other
        public bool IsTeaching { get; set; } = true;

        public int SeniorityOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<FacultyQualification> Qualifications { get; set; } = new List<FacultyQualification>();
        public ICollection<FacultyExperience> Experiences { get; set; } = new List<FacultyExperience>();
        public ICollection<FacultyTraining> Trainings { get; set; } = new List<FacultyTraining>();
        public ICollection<FacultyPublication> Publications { get; set; } = new List<FacultyPublication>();
        public ICollection<FacultySubject> Subjects { get; set; } = new List<FacultySubject>();
        public ICollection<FacultyResearchGuidance> ResearchGuidances { get; set; } = new List<FacultyResearchGuidance>();
        public ICollection<FacultyBookPublication> BookPublications { get; set; } = new List<FacultyBookPublication>();
        public ICollection<FacultyConsultancy> Consultancies { get; set; } = new List<FacultyConsultancy>();
        public ICollection<FacultyPatent> Patents { get; set; } = new List<FacultyPatent>();
        public ICollection<FacultyProfessionalMembership> ProfessionalMemberships { get; set; } = new List<FacultyProfessionalMembership>();
        public PersonalDetail? PersonalDetail { get; set; }

    }

    public class FacultyDetails
    {
        // kept for reference, actual split below
    }

    public class PersonalDetail : BaseEntity
    {
        public int PersonalDetailId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        // View only after RBAC - cannot be changed by faculty
        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        // Added for Mandatory Disclosure
        public DateTime? DateOfBirth { get; set; }
    }

    public class FacultyQualification : BaseEntity
    {
        public int FacultyQualificationId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        // PhD / Pursuing PhD / ME/MTech / BE/BTech
        [MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? University { get; set; }

        [MaxLength(20)]
        public string? Year { get; set; }

        public string? Specialization { get; set; }
    }

    public class FacultyExperience : BaseEntity
    {
        public int FacultyExperienceId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Organization { get; set; } = string.Empty;

        // Changed: Duration string → FromDate + ToDate
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Auto-calculated in VM/View
        // ToDate null = "Present"
    }

    public class FacultyTraining : BaseEntity
    {
        public int FacultyTrainingId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? TrainingType { get; set; }   

        [MaxLength(300)]
        public string? OrganizedBy { get; set; }

        // Changed: Date string → FromDate + ToDate
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class FacultyPublication : BaseEntity
    {
        public int FacultyPublicationId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        public int SrNo { get; set; }
        public string Title { get; set; } = string.Empty;
        // Added for Mandatory Disclosure
        // National / International / Conference
        [MaxLength(50)]
        public string Type { get; set; } = "International";

        [MaxLength(300)]
        public string? JournalName { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [MaxLength(500)]
        public string? CoAuthors { get; set; }
    }
    // ── Mandatory Disclosure Purpose ──────────────────────────────────────
    public class FacultySubject : BaseEntity
    {
        public int FacultySubjectId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [Required, MaxLength(300)]
        public string SubjectName { get; set; } = string.Empty;

        // UG / PG
        [MaxLength(10)]
        public string Level { get; set; } = "UG";

        public int DisplayOrder { get; set; } = 0;
    }

   public class FacultyResearchGuidance : BaseEntity
    {
        public int FacultyResearchGuidanceId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        // Masters / PhD
        [MaxLength(20)]
        public string Level { get; set; } = "Masters";

        public int Ongoing { get; set; } = 0;
        public int Completed { get; set; } = 0;
    }

    public class FacultyBookPublication : BaseEntity
    {
        public int FacultyBookPublicationId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        public int SrNo { get; set; } = 1;

        [Required, MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Publisher { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class FacultyConsultancy : BaseEntity
    {
        public int FacultyConsultancyId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [Required, MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Client { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [MaxLength(100)]
        public string? Amount { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class FacultyPatent : BaseEntity
    {
        public int FacultyPatentId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [Required, MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ApplicationNo { get; set; }

        [MaxLength(10)]
        public string? GrantedYear { get; set; }

        // Filed / Published / Granted
        [MaxLength(50)]
        public string Status { get; set; } = "Filed";

        public int DisplayOrder { get; set; } = 0;
    }

    public class FacultyProfessionalMembership : BaseEntity
    {
        public int FacultyProfessionalMembershipId { get; set; }
        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        [Required, MaxLength(300)]
        public string Community { get; set; } = string.Empty;

        // Member / Life Member / Fellow
        [MaxLength(50)]
        public string MembershipType { get; set; } = "Member";

        public int DisplayOrder { get; set; } = 0;
    }

}