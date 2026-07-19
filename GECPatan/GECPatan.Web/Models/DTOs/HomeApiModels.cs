namespace GECPatan.Web.Models.Dtos
{
    // Mirrors GECPatan.Api.DTOs.SiteSettingsDTO (from GET /api/home/settings).
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
        public string? MapEmbedUrl { get; set; }
        public string? MapLatitude { get; set; }
        public string? MapLongitude { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.SliderDTO (from GET /api/home/slider).
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

    // Mirrors GECPatan.Api.DTOs.MarqueeDTO (from GET /api/home/marquee).
    // The API already resolves the final URL server-side (Link) and already
    // filters to IsVisible + within ValidFrom/ValidTo, so nothing else needs
    // to be re-checked client-side.
    public class MarqueeDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none"; // "internal" | "dynamic" | "external" | "file" | "none"
        public string? Link { get; set; }
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.TestimonialDTO (from GET /api/home/testimonials).
    public class TestimonialDTO
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? PassoutYear { get; set; }
        public string TestimonialText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.TopRecruiterDTO (from GET /api/home/toprecruiters).
    public class TopRecruiterDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.HomeNewsDTO (from GET /api/home/news).
    public class HomeNewsDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PublishDate { get; set; }
        public bool ShowInMarquee { get; set; }
        public string? Link { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.HomeStatsDTO (from GET /api/home/stats).
    public class HomeStatsDTO
    {
        public int DepartmentCount { get; set; }
        public int FacultyCount { get; set; }
        public int StudentClubCount { get; set; }
        public int? LatestPlacementYear { get; set; }
        public int? LatestTotalPlaced { get; set; }
        public string? LatestHighestPackage { get; set; }

        // Feature highlight icons (Admin sets these under Home.Feature1.Text/.Icon etc.)
        // Icon values are font-awesome class strings, e.g. "fa fa-graduation-cap".
        public string? Feature1Text { get; set; }
        public string? Feature1Icon { get; set; }
        public string? Feature2Text { get; set; }
        public string? Feature2Icon { get; set; }
        public string? Feature3Text { get; set; }
        public string? Feature3Icon { get; set; }
        public string? Feature4Text { get; set; }
        public string? Feature4Icon { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.PrincipalMessageDTO (from GET /api/home/principal).
    public class PrincipalMessageDTO
    {
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public string? PhotoPath { get; set; }
        public string? Message { get; set; }
        public string? Institute { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.HomeActivityDTO (from GET /api/home/activities).
    public class HomeActivityDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EventDate { get; set; }
        public int? Year { get; set; }
        public int? CommitteeId { get; set; }
        public int? DeptId { get; set; }
        public int? ClubId { get; set; }
        public string? Link { get; set; }
        public string? ThumbnailPath { get; set; }
    }
}
