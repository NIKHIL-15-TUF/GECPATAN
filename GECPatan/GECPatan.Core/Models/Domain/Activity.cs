using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    public class Activity : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? EventDate { get; set; }
        public string? EventTime { get; set; }
        public int? Year { get; set; }

        public string? TargetStudents { get; set; }
        public string? Keywords { get; set; }

        // Links
        public string? ExternalLink { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }

        // Display
        public bool IsVisible { get; set; } = true;
        public bool IsFile { get; set; } = true;

        // Belongs to
        public int? DeptId { get; set; }
        public int? CommitteeId { get; set; }
        public int? ClubId { get; set; }

        // Navigation
        [ValidateNever]
        public ICollection<ActivityImage> Images { get; set; }
            = new List<ActivityImage>();

        [ValidateNever]
        public ICollection<ActivityFile> Files { get; set; }
            = new List<ActivityFile>();
    }

    // =============================================
    // ACTIVITY IMAGE
    // =============================================
    public class ActivityImage : BaseEntity
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        // FK
        public int ActivityId { get; set; }

        [ValidateNever]
        public Activity? Activity { get; set; }
    }

    // =============================================
    // ACTIVITY FILE
    // =============================================
    public class ActivityFile : BaseEntity
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        public string FilePath { get; set; } = string.Empty;

        [MaxLength(20)]
        public string FileType { get; set; } = "PDF";

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int ActivityId { get; set; }

        [ValidateNever]
        public Activity? Activity { get; set; }
    }
}