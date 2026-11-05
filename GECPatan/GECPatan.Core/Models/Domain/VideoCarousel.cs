using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    public class VideoCarousel : BaseEntity
    {
        [Key]
        public int ID { get; set; }

        public int OrderNo { get; set; }

        public bool IsShow { get; set; } = true;

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string VideoUrl { get; set; } = string.Empty;

        public string? Image { get; set; }
    }
}