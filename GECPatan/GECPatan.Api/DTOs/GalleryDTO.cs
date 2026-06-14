namespace GECPatan.Api.DTOs
{
    // ── GALLERY (for /api/gallery) ────
    public class GalleryImageDTO
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string? Category { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Gallery categories summary
    public class GalleryCategoryDTO
    {
        public string Category { get; set; } = string.Empty;
        public int ImageCount { get; set; }
        public string? CoverImage { get; set; } // first image in category
        public List<GalleryImageDTO> Images { get; set; } = new();
    }
}