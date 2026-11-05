
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    public class NotableAlumni : BaseEntity
    {
        [Key]
        public int ID { get; set; }

        public int OrderNo { get; set; }

        public bool IsShow { get; set; } = true;

        public bool IsIframe { get; set; } = false;

        public string? IFrame { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Image { get; set; }
    }
}
