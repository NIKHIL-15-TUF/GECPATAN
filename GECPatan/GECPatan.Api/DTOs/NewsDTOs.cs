namespace GECPatan.Api.DTOs
{
    // ── LIST (for /api/news, paged) ────────────────────────
    public class NewsListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public string? PublishDate { get; set; }
        public bool ShowInMarquee { get; set; }
        public string? Link { get; set; } // resolved external/internal link
    }

    // ── DETAIL (for /api/news/{id}) ────────────────────────
    public class NewsDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BannerImagePath { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? PublishDate { get; set; }
        public string? ExternalLink { get; set; }
        public string? Link { get; set; }

        public List<NewsImageDTO> Images { get; set; } = new();
        public List<NewsFileDTO> Files { get; set; } = new();
    }

    public class NewsImageDTO
    {
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class NewsFileDTO
    {
        public string? Title { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    // ── NEWS LETTERS (for /api/news/letters) ───────────────
    public class NewsLetterDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? DownloadName { get; set; }
        public string? ThumbnailPath { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── ACHIEVEMENTS (for /api/achievements) ───────────────
    public class AchievementDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string? Date { get; set; }
        public int? Year { get; set; }
        public string? TypeName { get; set; } // Academic/Sports/Cultural/NSS/Other
        public int? DeptId { get; set; }
        public string? DeptName { get; set; }
        public int? CommitteeId { get; set; }
    }
}