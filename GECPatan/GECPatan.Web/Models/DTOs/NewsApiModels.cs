namespace GECPatan.Web.Models.Dtos
{
    // Mirrors GECPatan.Api.DTOs.NewsLetterDTO (GET /api/news/letters).
    public class NewsLetterDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Relative path on GECPatan.Api's own static file host -- resolve
        // against Api:BaseUrl (same ResolveApiFileUrl() pattern used on the
        // Home/Department/Achievement pages), never use as-is.
        public string? FilePath { get; set; }

        // Suggested filename for a "Download" link/attribute. Not a URL.
        public string? DownloadName { get; set; }

        // Also on the API's static file host -- resolve the same way as FilePath.
        public string? ThumbnailPath { get; set; }

        public int DisplayOrder { get; set; }
    }
}
