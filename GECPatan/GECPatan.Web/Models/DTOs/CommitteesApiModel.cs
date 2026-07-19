namespace GECPatan.Web.Models.Dtos
{
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

        public string? TabAbout { get; set; }
        public string? TabVisionMission { get; set; }
        public string? TabObjectives { get; set; }
        public string? TabMembers { get; set; }
        public string? TabActivities { get; set; }
        public string? TabDocuments { get; set; }
        public string? TabLink { get; set; }

        public List<string> Visions { get; set; } = new();
        public List<string> Missions { get; set; } = new();
        public List<string> Objectives { get; set; } = new();
        public List<string> SubObjectives { get; set; } = new();

        public List<CommitteeMemberDTO> Members { get; set; } = new();
        public List<CommitteeMemberGroupDTO> AdditionalMemberGroups { get; set; } = new();

        // Same shape as Department's DynamicSectionDTO (see DepartmentDtos.cs) --
        // reused here rather than duplicated.
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }

    public class CommitteeMemberDTO
    {
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Image { get; set; }
    }

    public class CommitteeMemberGroupDTO
    {
        public string? CommitteeTitle { get; set; }
        public List<CommitteeMemberDTO> Members { get; set; } = new();
    }
}