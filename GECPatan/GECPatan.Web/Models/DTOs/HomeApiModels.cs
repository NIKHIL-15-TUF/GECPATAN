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

    // Mirrors GECPatan.Api.DTOs.MarqueeDTO (from GET /api/home/marquee --
    // horizontal ticker below the slider ONLY; the API already filters to
    // HorizontalMarquee == true).
    public class MarqueeDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none"; // internal/dynamic/external/file/none

        // Resolved for internal/dynamic/external only -- null for "file".
        public string? Link { get; set; }

        // Only set when LinkType == "file". This is a relative path on
        // GECPatan.Api's own static file host -- MUST be resolved against
        // Api:BaseUrl before use (see ResolveApiFileUrl in the view), same
        // as image paths. Using it as-is is the bug that made ticker/update
        // files fail to open.
        public string? FilePath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Mirrors GECPatan.Api.DTOs.UpdateItemDTO (from GET /api/home/updates --
    // the vertical "UPDATES" list). Merges Marquee items with
    // HorizontalMarquee == false and NewsItem items with ShowInMarquee == true.
    public class UpdateItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none";
        public string? Link { get; set; }
        public string? FilePath { get; set; } // resolve against Api:BaseUrl, same as MarqueeDTO
        public string Source { get; set; } = string.Empty; // "marquee" | "news"
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
    // Unfiltered "latest news" feed -- kept for potential reuse elsewhere;
    // the homepage no longer uses this for the Updates list (see UpdateItemDTO).
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
