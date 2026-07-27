using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    // Replaces the old HeaderVM the legacy footer.cshtml expected.
    // Populated from GET /api/home/settings + GET /api/menu/footer.
    public class FooterViewModel
    {
        public List<MenuItemDTO> FooterMenu { get; set; } = new();
        public string ApiBaseUrl { get; set; } = string.Empty;   
        public string? ContactNo { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? MapEmbedUrl { get; set; }

        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? InstagramUrl { get; set; }
    }
}
