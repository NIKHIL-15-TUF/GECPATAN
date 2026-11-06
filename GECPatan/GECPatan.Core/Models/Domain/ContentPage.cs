using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GECPatan.Core.Models.Domain
{
    // CONTENT PAGE (standalone TinyMCE pages)
    public class ContentPage : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? TitleImageUrl { get; set; }

        // Auto generated from title e.g. nirf-report-2025
        [MaxLength(300)]
        public string Slug { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }

        //[MaxLength(200)]
        //public string? CreatedBy { get; set; }

        public bool IsVisible { get; set; } = true;

        public ICollection<ContentPageImage> Images { get; set; } = new List<ContentPageImage>();
    }
    public class ContentPageImage : BaseEntity
    {
        public int Id { get; set; }

        public int ContentPageId { get; set; }
        public ContentPage ContentPage { get; set; } = null!;

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Caption { get; set; }

        public int DisplayOrder { get; set; }
    }

}
