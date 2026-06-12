using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    // NEWS ITEM
    public class NewsItem : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? BannerImagePath { get; set; }
        public string? ThumbnailPath { get; set; }

        public DateTime? PublishDate { get; set; }

        // Optional link to external page
        public string? ExternalLink { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool ShowInMarquee { get; set; } = false;

        // Navigation
        [ValidateNever]
        public ICollection<NewsItemImage> Images { get; set; }
            = new List<NewsItemImage>();

        [ValidateNever]
        public ICollection<NewsItemFile> Files { get; set; }
            = new List<NewsItemFile>();
    }

    // NEWS ITEM IMAGE
    public class NewsItemImage : BaseEntity
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        // FK
        public int NewsItemId { get; set; }

        [ValidateNever]
        public NewsItem? NewsItem { get; set; }
    }
    // NEWS ITEM FILE
    public class NewsItemFile : BaseEntity
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        public string FilePath { get; set; } = string.Empty;

        [MaxLength(20)]
        public string FileType { get; set; } = "PDF";

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int NewsItemId { get; set; }

        [ValidateNever]
        public NewsItem? NewsItem { get; set; }
    }
    // NEWS LETTER
    public class NewsLetter : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }
        public string? DownloadName { get; set; }
        public string? ThumbnailPath { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}