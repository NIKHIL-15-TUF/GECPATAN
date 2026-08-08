using System.ComponentModel.DataAnnotations;
using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class ContactUsPageViewModel
    {
        public ContactSettingsDTO Settings { get; set; } = new();
        public ContactFormVM Form { get; set; } = new();
    }

    // Mirrors GECPatan.Api's ContactSubmitRequestDTO validation rules, so the
    // user gets the same feedback immediately server-side (in addition to
    // the client-side checks in Index.cshtml) without a round trip to the Api.
    public class ContactFormVM
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a subject.")]
        [StringLength(300, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 300 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your message.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 4000 characters.")]
        public string Message { get; set; } = string.Empty;
    }

    // JSON shape returned by ContactUsController.Submit for the page's AJAX handler.
    public class ContactSubmitResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
