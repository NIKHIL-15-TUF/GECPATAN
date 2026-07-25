using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class GalleryViewModel
    {
        public List<GalleryImageDTO> Images { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? ApiBaseUrl { get; set; }
    }
}
