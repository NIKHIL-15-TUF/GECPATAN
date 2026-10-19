using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GECPatan.Core.Models.Domain
{
    public enum CarouselType { 
        NotableAlumni = 1, LatestShot = 2 
    }
    public class CarouselItem : BaseEntity
    {
        public int Id { get; set; }
        public CarouselType Type { get; set; }
        [MaxLength(300)] public string? Title { get; set; }
        public string? ImagePath { get; set; }
        public bool IsVideo { get; set; }
        [MaxLength(500)] public string? VideoUrl { get; set; }   // YouTube URL, NOT raw iframe HTML
        public DateTime? UploadDate { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
