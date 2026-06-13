namespace GECPatan.Api.DTOs
{
    // ── PRINCIPAL PROFILE (for /api/principal) ─────────────
    public class PrincipalProfileDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? PhotoPath { get; set; }
        public string? Message { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? DateOfJoiningInstitute { get; set; }
        public string? DateOfJoiningDept { get; set; }

        public List<PrincipalQualificationDTO> Qualifications { get; set; } = new();
        public List<PrincipalExperienceDTO> Experiences { get; set; } = new();
        public List<PrincipalPublicationDTO> Publications { get; set; } = new();
        public List<PrincipalBookPublicationDTO> BookPublications { get; set; } = new();
        public List<PrincipalExpertTalkDTO> ExpertTalks { get; set; } = new();
        public List<PrincipalAchievementDTO> Achievements { get; set; } = new();
        public List<PrincipalMembershipDTO> Memberships { get; set; } = new();

        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }

    public class PrincipalQualificationDTO
    {
        public string Degree { get; set; } = string.Empty;
        public string? University { get; set; }
        public string? Year { get; set; }
        public string? Result { get; set; }
    }

    public class PrincipalExperienceDTO
    {
        public string Designation { get; set; } = string.Empty;
        public string? Organization { get; set; }
        public string? Place { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; } // null = Present
    }

    public class PrincipalPublicationDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? JournalOrConference { get; set; }
        public string Type { get; set; } = "Journal";
        public string? DOI { get; set; }
        public string? Year { get; set; }
    }

    public class PrincipalBookPublicationDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? BookCode { get; set; }
        public string? University { get; set; }
        public string? Branch { get; set; }
        public string? Semester { get; set; }
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }
        public string? ContentTopics { get; set; }
    }

    public class PrincipalExpertTalkDTO
    {
        public string? Year { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Place { get; set; }
        public string? Details { get; set; }
    }

    public class PrincipalAchievementDTO
    {
        public string AchievementText { get; set; } = string.Empty;
        public string? Year { get; set; }
    }

    public class PrincipalMembershipDTO
    {
        public string MembershipText { get; set; } = string.Empty;
    }
}