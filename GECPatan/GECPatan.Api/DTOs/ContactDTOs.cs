using System.ComponentModel.DataAnnotations;

namespace GECPatan.Api.DTOs
{
    // GET /api/contact/settings — public-facing contact info + form config.
    // Internal-only settings (support/sender email, SMTP credentials) are
    // deliberately not part of this DTO.
    public class ContactSettingsDTO
    {
        public string? CollegeName { get; set; } = "Government Engineering College, Patan";
        public string? Address { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Email1 { get; set; }
        public string? Email2 { get; set; }
        public string? OfficeHours { get; set; }
        public string? MapEmbedUrl { get; set; }
        public List<string> AllowedCategories { get; set; } = new();
    }

    // POST /api/contact/submit body.
    public class ContactSubmitRequestDTO
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone(ErrorMessage = "Enter a valid phone number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(300, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 300 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 4000 characters")]
        public string Message { get; set; } = string.Empty;
    }
}
