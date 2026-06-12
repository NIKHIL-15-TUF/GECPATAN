using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    // =============================================
    // PRINCIPAL (main record)
    // Only ONE active at a time
    // On transfer → IsActive = false, new one added
    // =============================================
    public class Principal : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; } = "Principal";

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        public string? PhotoPath { get; set; }

        // Message shown on home page + principal page
        public string? Message { get; set; }

        [MaxLength(500)]
        public string? AreaOfInterest { get; set; }

        // Date joined as principal of THIS institute
        public DateTime? DateOfJoiningInstitute { get; set; }

        // Date joined current department
        public DateTime? DateOfJoiningDept { get; set; }

        // Only ONE principal is active at a time
        public bool IsActive { get; set; } = true;

        // Transfer info — filled when deactivated
        [MaxLength(300)]
        public string? TransferNote { get; set; }

        public DateTime? TransferDate { get; set; }

        // Navigation
        public ICollection<PrincipalQualification> Qualifications { get; set; } = new List<PrincipalQualification>();
        public ICollection<PrincipalExperience> Experiences { get; set; } = new List<PrincipalExperience>();
        public ICollection<PrincipalPublication> Publications { get; set; } = new List<PrincipalPublication>();
        public ICollection<PrincipalBookPublication> BookPublications { get; set; } = new List<PrincipalBookPublication>();
        public ICollection<PrincipalExpertTalk> ExpertTalks { get; set; } = new List<PrincipalExpertTalk>();
        public ICollection<PrincipalAchievement> Achievements { get; set; } = new List<PrincipalAchievement>();
        public ICollection<PrincipalMembership> Memberships { get; set; } = new List<PrincipalMembership>();
    }

    // =============================================
    // QUALIFICATIONS
    // =============================================
    public class PrincipalQualification : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required, MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? University { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [MaxLength(100)]
        public string? Result { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    // =============================================
    // EXPERIENCE
    // =============================================
    public class PrincipalExperience : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required, MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Organization { get; set; }

        [MaxLength(200)]
        public string? Place { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        // ToDate null = Present

        public int DisplayOrder { get; set; } = 0;
    }

    // =============================================
    // PUBLICATIONS (Journal / Conference)
    // =============================================
    public class PrincipalPublication : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? JournalOrConference { get; set; }

        // "Journal" or "Conference"
        [MaxLength(20)]
        public string Type { get; set; } = "Journal";

        [MaxLength(200)]
        public string? DOI { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    // =============================================
    // BOOK PUBLICATIONS
    // =============================================
    public class PrincipalBookPublication : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required, MaxLength(300)]
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

    // =============================================
    // EXPERT TALKS / GUEST LECTURES
    // =============================================
    public class PrincipalExpertTalk : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [MaxLength(10)]
        public string? Year { get; set; }

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Place { get; set; }

        public string? Details { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    // =============================================
    // ACHIEVEMENTS
    // =============================================
    public class PrincipalAchievement : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required]
        public string AchievementText { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Year { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    // =============================================
    // MEMBERSHIPS / PROFESSIONAL BODIES
    // =============================================
    public class PrincipalMembership : BaseEntity
    {
        public int Id { get; set; }
        public int PrincipalId { get; set; }

        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }

        [Required]
        public string MembershipText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }
}
