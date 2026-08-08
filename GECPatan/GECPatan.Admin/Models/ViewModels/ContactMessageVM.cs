using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── LIST ITEM ─────────────────────────────────────────
    public class ContactMessageListItemVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsReplied { get; set; }
        public string SubmittedDate { get; set; } = string.Empty;
    }

    // ── FILTER + LIST PAGE ────────────────────────────────
    public class ContactMessageFilterVM
    {
        [Display(Name = "Search")]
        public string? Search { get; set; } // matches name, email, or subject

        public string? Category { get; set; }

        // All / Unread / Read / Replied / NotReplied
        public string? Status { get; set; }

        public List<string> Categories { get; set; } = new();
        public List<ContactMessageListItemVM> Messages { get; set; } = new();

        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
    }

    // ── DETAIL PAGE ───────────────────────────────────────
    public class ContactMessageDetailVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public bool IsReplied { get; set; }
        public string? RepliedDate { get; set; }
        public string? RepliedBy { get; set; }
        public string? ReplyMessage { get; set; }

        public string SubmittedDate { get; set; } = string.Empty;
        public string? IpAddress { get; set; }

        // Bound from the reply form on the same page
        [Display(Name = "Your Reply")]
        public string? ReplyBody { get; set; }
    }
}
