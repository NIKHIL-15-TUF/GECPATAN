using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/contact")]
    public class ContactController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly ILogger<ContactController> _logger;

        // Fallback defaults, used only if the corresponding SiteSetting has
        // never been saved from the Admin Portal — keeps the form usable out
        // of the box even before an admin visits Contact Info settings.
        private static readonly string[] DefaultCategories =
        {
            "General Inquiry", "Admission Inquiry", "Feedback", "Suggestion",
            "Website Issue", "Technical Issue", "Complaint", "Other"
        };

        private const string DefaultSuccessMessage =
            "Thank you for contacting us. Your message has been submitted successfully. Our team will review it and contact you if required.";

        private const string DefaultFailureMessage =
            "Sorry, we couldn't submit your message right now. Please try again in a few minutes.";

        public ContactController(
            ApplicationDbContext context,
            IEmailService email,
            ILogger<ContactController> logger)
        {
            _context = context;
            _email = email;
            _logger = logger;
        }

        // GET /api/contact/settings
        // College contact info + allowed categories for the public Contact
        // Us page — sourced from the same "Contact_*" SiteSettings the
        // Admin Portal's Contact Info screen manages, so the two never drift.
        [HttpGet("settings")]
        public async Task<ActionResult<ApiResponse<ContactSettingsDTO>>> GetSettings()
        {
            var settings = await LoadContactSettingsAsync();
            string? Get(string key) => settings.GetValueOrDefault(key);

            var data = new ContactSettingsDTO
            {
                CollegeName = Get("Contact_CollegeName"),
                Address = Get("Contact_Address"),
                Phone1 = Get("Contact_Phone1"),
                Phone2 = Get("Contact_Phone2"),
                Email1 = Get("Contact_Email1"),
                Email2 = Get("Contact_Email2"),
                OfficeHours = Get("Contact_OfficeHours"),
                MapEmbedUrl = Get("Contact_MapEmbedUrl"),
                AllowedCategories = ParseCategories(Get("Contact_AllowedCategories"))
            };

            return Ok(ApiResponse<ContactSettingsDTO>.Ok(data));
        }

        // POST /api/contact/submit
        // Validates, saves, and emails a notification for a new Contact Us
        // submission. Rate-limited per client IP — see the "ContactForm"
        // policy in Program.cs (configurable via "RateLimiting:ContactForm").
        [HttpPost("submit")]
        [EnableRateLimiting("ContactForm")]
        public async Task<ActionResult<ApiResponse<object>>> Submit(
            [FromBody] ContactSubmitRequestDTO request, CancellationToken ct)
        {
            var settings = await LoadContactSettingsAsync(ct);
            string? Get(string key) => settings.GetValueOrDefault(key);

            var successMessage = string.IsNullOrWhiteSpace(Get("Contact_SuccessMessage"))
                ? DefaultSuccessMessage : Get("Contact_SuccessMessage")!;
            var failureMessage = string.IsNullOrWhiteSpace(Get("Contact_FailureMessage"))
                ? DefaultFailureMessage : Get("Contact_FailureMessage")!;

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                return BadRequest(ApiResponse<object>.Fail(firstError ?? failureMessage));
            }

            var allowedCategories = ParseCategories(Get("Contact_AllowedCategories"));
            if (!allowedCategories.Contains(request.Category, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.Fail("Please select a valid category."));
            }

            var message = new ContactMessage
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Category = request.Category.Trim(),
                Subject = request.Subject.Trim(),
                Message = request.Message.Trim(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            try
            {
                _context.ContactMessages.Add(message);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Contact Us submission from {Email}", request.Email);
                return StatusCode(500, ApiResponse<object>.Fail(failureMessage));
            }

            // Best-effort notification — a failed email must never turn a
            // successfully saved message into a failure response to the user.
            var supportEmail = Get("Contact_SupportEmail");
            if (!string.IsNullOrWhiteSpace(supportEmail))
            {
                var sent = await _email.SendAsync(
                    toEmail: supportEmail!,
                    subject: $"[Contact Us] {message.Category}: {message.Subject}",
                    htmlBody: BuildNotificationEmailBody(message),
                    fromEmail: Get("Contact_SenderEmail"),
                    fromDisplayName: Get("Contact_CollegeName"),
                    replyToEmail: message.Email,
                    ct: ct);

                if (!sent)
                {
                    _logger.LogWarning(
                        "Contact Message {Id} saved but the notification email to {SupportEmail} failed to send.",
                        message.Id, supportEmail);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Contact_SupportEmail is not configured — skipping notification email for Contact Message {Id}.",
                    message.Id);
            }

            return Ok(ApiResponse<object>.Ok(new { id = message.Id }, successMessage));
        }

        private async Task<Dictionary<string, string?>> LoadContactSettingsAsync(CancellationToken ct = default)
            => await _context.SiteSettings
                .Where(s => s.Group == "Contact")
                .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        private static List<string> ParseCategories(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DefaultCategories.ToList();

            var items = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            return items.Count > 0 ? items : DefaultCategories.ToList();
        }

        private static string BuildNotificationEmailBody(ContactMessage m) => $@"
            <h2>New Contact Us Submission</h2>
            <table cellpadding='6' cellspacing='0' style='border-collapse:collapse;'>
                <tr><td><b>Name</b></td><td>{WebUtility.HtmlEncode(m.FullName)}</td></tr>
                <tr><td><b>Email</b></td><td>{WebUtility.HtmlEncode(m.Email)}</td></tr>
                <tr><td><b>Phone</b></td><td>{WebUtility.HtmlEncode(m.Phone ?? "-")}</td></tr>
                <tr><td><b>Category</b></td><td>{WebUtility.HtmlEncode(m.Category)}</td></tr>
                <tr><td><b>Subject</b></td><td>{WebUtility.HtmlEncode(m.Subject)}</td></tr>
                <tr><td valign='top'><b>Message</b></td><td>{WebUtility.HtmlEncode(m.Message).Replace("\n", "<br/>")}</td></tr>
                <tr><td><b>Submitted</b></td><td>{m.CreatedDate:dd MMM yyyy, hh:mm tt}</td></tr>
            </table>
            <p style='color:#888;font-size:12px;'>Reply directly from the Admin Portal → Contact Messages to respond to this person.</p>";
    }
}
