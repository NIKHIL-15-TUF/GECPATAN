using System.ComponentModel.DataAnnotations;
using GECPatan.Core.Models.Domain;

namespace GECPatan.Admin.Models.ViewModels
{
    public class CarouselVM
    {
        public int Id { get; set; }

        public CarouselType Type { get; set; }

        [MaxLength(300)]
        public string? Title { get; set; }

        public string? ExistingImagePath { get; set; }

        [Display(Name = "This is a video")]
        public bool IsVideo { get; set; }

        [MaxLength(500)]
        [Display(Name = "Video URL (YouTube or Vimeo)")]
        public string? VideoUrl { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Upload date")]
        public DateTime? UploadDate { get; set; }

        public int DisplayOrder { get; set; }

        [Display(Name = "Visible")]
        public bool IsVisible { get; set; } = true;
    }
}
