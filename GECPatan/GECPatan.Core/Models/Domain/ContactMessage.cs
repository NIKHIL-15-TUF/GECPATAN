using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    /// <summary>
    /// A single Contact Us form submission from the public website.
    /// Read/Replied status is a single shared mailbox state (not per-admin-user),
    /// matching how the Admin Portal presents this as one shared inbox.
    /// </summary>
    public class ContactMessage : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// One of the admin-configured allowed categories (see SiteSetting
        /// "Contact_AllowedCategories"). Stored as free text rather than an
        /// enum so new categories can be added from the Admin Portal without
        /// a code change/deploy.
        /// </summary>
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public bool IsReplied { get; set; } = false;
        public DateTime? RepliedDate { get; set; }

        [MaxLength(200)]
        public string? RepliedBy { get; set; }

        /// <summary>Body of the most recent reply sent from the Admin Portal, kept for reference.</summary>
        public string? ReplyMessage { get; set; }

        /// <summary>Submitter's IP address at the time of submission — useful for abuse/rate-limit investigation.</summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }
    }
}