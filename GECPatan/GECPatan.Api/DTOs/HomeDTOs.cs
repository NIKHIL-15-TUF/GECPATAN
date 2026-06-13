namespace GECPatan.Api.DTOs
{
    public class SliderDTO
    {
        public int Id { get; set; }
        public string? ImagePath { get; set; }
        public string? H3Text { get; set; }
        public string? H4Text { get; set; }
        public string? H5Text { get; set; }
        public string? Anchor1Text { get; set; }
        public string? Anchor1Link { get; set; }
        public string? Anchor2Text { get; set; }
        public string? Anchor2Link { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TestimonialDTO
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? PassoutYear { get; set; }
        public string TestimonialText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class TopRecruiterDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public int DisplayOrder { get; set; }
    }
    public class MarqueeDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none";
        public string? Link { get; set; }   // resolved URL
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }
    public class HomeNewsDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PublishDate { get; set; }
        public bool ShowInMarquee { get; set; }
        public string? Link { get; set; }
    }

    public class HomeStatsDTO
    {
        public int DepartmentCount { get; set; }
        public int FacultyCount { get; set; }
        public int StudentClubCount { get; set; }
        public int? LatestPlacementYear { get; set; }
        public int? LatestTotalPlaced { get; set; }
        public string? LatestHighestPackage { get; set; }
    }

    public class SiteSettingsDTO
    {
        public string? Vision { get; set; }
        public string? Mission { get; set; }
        public string? PrincipalName { get; set; }
        public string? PrincipalDesignation { get; set; }
        public string? PrincipalMessage { get; set; }
        public string? PrincipalPhoto { get; set; }
        public string? EstablishedYear { get; set; }
        public string? CollegeTagline { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}