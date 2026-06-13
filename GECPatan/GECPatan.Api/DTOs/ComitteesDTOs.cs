namespace GECPatan.Api.DTOs
{
    // ── LIST (for /api/committees) ─────────────────────────
    public class CommitteeListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? TitleImagePath { get; set; }
        public int DisplayOrder { get; set; }
        public int MemberCount { get; set; }
        public int ActivityCount { get; set; }
    }

    // ── DETAIL (for /api/committees/{id}) ──────────────────
    public class CommitteeDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? Tagline { get; set; }
        public string? Measures { get; set; }
        public string? Message { get; set; }

        public string? TitleImagePath { get; set; }
        public string? TitleImageCSSClass { get; set; }
        public string? MeasureImagePath { get; set; }
        public string? SubObjImagePath { get; set; }
        public string? BulletPointsImagePath { get; set; }
        public string? PageFlyerPath { get; set; }

        public string? BlogLink { get; set; }
        public string? Link { get; set; }
        public string? Account { get; set; }
        public string? NationalTaskForce { get; set; }

        public bool ShowDocument { get; set; }
        public bool TableView { get; set; }

        // Tab title overrides (frontend uses these as labels)
        public string TabAbout { get; set; } = "About";
        public string TabVisionMission { get; set; } = "Vision & Mission";
        public string TabObjectives { get; set; } = "Objectives";
        public string TabMembers { get; set; } = "Members";
        public string TabActivities { get; set; } = "Activities";
        public string TabDocuments { get; set; } = "Documents";
        public string TabLink { get; set; } = "Register Grievance";

        public List<string> Visions { get; set; } = new();
        public List<string> Missions { get; set; } = new();
        public List<string> Objectives { get; set; } = new();
        public List<string> SubObjectives { get; set; } = new();

        public List<CommitteeMemberDTO> Members { get; set; } = new();
        public List<AdditionalMemberGroupDTO> AdditionalMemberGroups { get; set; } = new();

        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }

    // ── MEMBERS ─────────────────────────────────────────────
    public class CommitteeMemberDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? ImagePath { get; set; }
        public string? Department { get; set; }
        public int DisplayOrder { get; set; }
        // Email/Contact intentionally excluded from public API
    }

    public class AdditionalMemberGroupDTO
    {
        public int Id { get; set; }
        public string GroupTitle { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<AdditionalMemberDetailDTO> Members { get; set; } = new();
    }

    public class AdditionalMemberDetailDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? ImagePath { get; set; }
        public string? Department { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── ACTIVITIES (for /api/committees/{id}/activities) ───
    public class ActivityDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EventDate { get; set; }
        public string? EventTime { get; set; }
        public int? Year { get; set; }
        public string? TargetStudents { get; set; }
        public string? Keywords { get; set; }
        public string? ExternalLink { get; set; }
        public string? Link { get; set; } // resolved internal link

        public List<ActivityImageDTO> Images { get; set; } = new();
        public List<ActivityFileDTO> Files { get; set; } = new();
    }

    public class ActivityImageDTO
    {
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ActivityFileDTO
    {
        public string? Title { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}