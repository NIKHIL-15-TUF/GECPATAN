using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class VideoCarouselVM
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Order Number")]
        public int OrderNo { get; set; }

        [Display(Name = "Show on Website")]
        public bool IsShow { get; set; } = true;

        [Required]
        [StringLength(500)]
        [Display(Name = "Video Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "YouTube Video / Shorts URL")]
        [Url]
        public string VideoUrl { get; set; } = string.Empty;

        [Display(Name = "Thumbnail Image")]
        public string? Image { get; set; }
    }
}   