namespace GECPatan.Web.Models.Dtos
{
    public class GalleryImageDTO
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string Category { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
