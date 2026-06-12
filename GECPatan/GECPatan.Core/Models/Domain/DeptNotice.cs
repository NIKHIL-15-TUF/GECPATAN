using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    public class DeptNotice : BaseEntity
    {
        public int Id { get; set; }

        public int DeptId { get; set; }

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        // Optional description / rich text
        public string? Description { get; set; }

        // PDF / Image / File
        public string? FilePath { get; set; }

        [MaxLength(20)]
        public string? FileType { get; set; } // PDF / Image / Link

        // External link (if no file)
        [MaxLength(500)]
        public string? ExternalLink { get; set; }

        // Validity window
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        // Who posted
        [MaxLength(200)]
        public string? PostedBy { get; set; }

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}