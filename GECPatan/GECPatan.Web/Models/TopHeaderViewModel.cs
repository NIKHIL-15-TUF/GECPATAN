using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    // Replaces the old HeaderVM the legacy topHeader.cshtml expected.
    // Populated from GET /api/home/settings + GET /api/menu/top.
    public class HeaderViewModel
    {
        public List<MenuItemDTO> TopMenu { get; set; } = new();

        public string? ContactNo { get; set; }
        public string? ContactEmail { get; set; }

        // No Logo field exists on SiteSettingsDTO today — kept as a static
        // asset path, same as the logo used in _Footer.cshtml.
        public string Logo { get; set; } = "~/images/GECP_LOGO.png";
    }
}
