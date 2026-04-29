using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Admin.Models.Domain
{
    // NOTIFICATION
    public class Notification
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        // null = all admins see it
        // set = only this user sees it
        [MaxLength(200)]
        public string? ForUserId { get; set; }

        // null = all roles
        // "HOD" = only HOD sees it
        // "SuperAdmin" = only SuperAdmin sees it
        [MaxLength(50)]
        public string? ForRole { get; set; }

        // Where to go when clicked
        [MaxLength(500)]
        public string? Link { get; set; }

        // Which module triggered it
        [MaxLength(100)]
        public string? Module { get; set; }

        // fas icon class e.g. "fa-user-plus"
        [MaxLength(50)]
        public string Icon { get; set; } = "fa-bell";

        // bootstrap color: success/info/warning/danger/primary
        [MaxLength(20)]
        public string IconColor { get; set; } = "info";

        // Who triggered this notification
        [MaxLength(200)]
        public string? TriggeredBy { get; set; }

        [MaxLength(50)]
        public string? TriggeredByRole { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Auto-delete after 30 days handled in service
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ICollection<NotificationRead> ReadBy { get; set; }
            = new List<NotificationRead>();
    }
    // NOTIFICATION READ — per user read state
    public class NotificationRead
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }

        [Required, MaxLength(200)]
        public string UserId { get; set; } = string.Empty;

        public DateTime ReadAt { get; set; } = DateTime.Now;
    }
}