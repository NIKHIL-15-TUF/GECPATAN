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
}
