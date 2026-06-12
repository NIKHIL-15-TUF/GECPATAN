using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    public class Achievement : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        // Date stored as string to match existing format (e.g. "2025/12/13")
        public string? Date { get; set; }
        public int? Year { get; set; }

        public string? Keywords { get; set; }

        // Type: 1=Academic, 2=Sports, 3=Cultural, 4=NSS, 5=Other
        public int Type { get; set; } = 1;

        // Belongs to
        public int? DeptId { get; set; }
        public string? DeptName { get; set; }
        public int? CommitteeId { get; set; }

        // Display
        public bool IsVisible { get; set; } = true;
    }
}