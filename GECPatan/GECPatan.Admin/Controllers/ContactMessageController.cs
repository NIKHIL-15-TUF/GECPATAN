using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class ContactMessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly ILogger<ContactMessageController> _logger;

        public ContactMessageController(
            ApplicationDbContext context,
            IEmailService email,
            ILogger<ContactMessageController> logger)
        {
            _context = context;
            _email = email;
            _logger = logger;
        }

        // ── LIST / SEARCH / FILTER ────────────────────────
        public async Task<IActionResult> Index(ContactMessageFilterVM filter)
        {
            ViewData["Title"] = "Contact Messages";

            filter.Categories = await _context.ContactMessages
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(m =>
                    m.FullName.Contains(term) ||
                    m.Email.Contains(term) ||
                    m.Subject.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(m => m.Category == filter.Category);

            switch (filter.Status)
            {
                case "Unread":
                    query = query.Where(m => !m.IsRead);
                    break;
                case "Read":
                    query = query.Where(m => m.IsRead);
                    break;
                case "Replied":
                    query = query.Where(m => m.IsReplied);
                    break;
                case "NotReplied":
                    query = query.Where(m => !m.IsReplied);
                    break;
            }

            filter.TotalCount = await _context.ContactMessages.CountAsync();
            filter.UnreadCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);

            filter.Messages = await query
                .OrderByDescending(m => m.CreatedDate)
                .Take(500) // consistent with AuditLogController's list cap
                .Select(m => new ContactMessageListItemVM
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    Category = m.Category,
                    Subject = m.Subject,
                    IsRead = m.IsRead,
                    IsReplied = m.IsReplied,
                    SubmittedDate = m.CreatedDate.ToString("dd MMM yyyy, hh:mm tt")
                })
                .ToListAsync();

            return View(filter);
        }

        // ── DETAIL (auto-marks read on open) ──────────────
        public async Task<IActionResult> Details(int id)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();

            if (!m.IsRead)
            {
                m.IsRead = true;
                await _context.SaveChangesAsync();
            }

            ViewData["Title"] = $"Message from {m.FullName}";

            return View(new ContactMessageDetailVM
            {
                Id = m.Id,
                FullName = m.FullName,
                Email = m.Email,
                Phone = m.Phone,
                Category = m.Category,
                Subject = m.Subject,
                Message = m.Message,
                IsRead = m.IsRead,
                IsReplied = m.IsReplied,
                RepliedDate = m.RepliedDate?.ToString("dd MMM yyyy, hh:mm tt"),
                RepliedBy = m.RepliedBy,
                ReplyMessage = m.ReplyMessage,
                SubmittedDate = m.CreatedDate.ToString("dd MMM yyyy, hh:mm tt"),
                IpAddress = m.IpAddress
            });
        }

        // ── REPLY (sends email from the configured support/sender account) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyBody)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();

            if (string.IsNullOrWhiteSpace(replyBody))
            {
                TempData["Error"] = "Please enter a reply message.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var settings = await _context.SiteSettings
                .Where(s => s.Group == "Contact")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var supportEmail = settings.GetValueOrDefault("Contact_SupportEmail");
            var senderEmail = settings.GetValueOrDefault("Contact_SenderEmail");
            var collegeName = settings.GetValueOrDefault("Contact_CollegeName");

            if (string.IsNullOrWhiteSpace(supportEmail) && string.IsNullOrWhiteSpace(senderEmail))
            {
                TempData["Error"] = "Support/Sender email is not configured yet — set it under Contact Info before sending replies.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var body = BuildReplyEmailBody(m.FullName, m.Subject, m.Message, replyBody, collegeName);

            var sent = await _email.SendAsync(
                toEmail: m.Email,
                subject: $"Re: {m.Subject}",
                htmlBody: body,
                fromEmail: string.IsNullOrWhiteSpace(senderEmail) ? supportEmail : senderEmail,
                fromDisplayName: collegeName,
                replyToEmail: supportEmail);

            if (!sent)
            {
                _logger.LogWarning("Failed to send reply email for Contact Message {Id} to {Email}", id, m.Email);
                TempData["Error"] = "Could not send the reply email. Please check the SMTP configuration and try again.";
                return RedirectToAction(nameof(Details), new { id });
            }

            m.IsReplied = true;
            m.IsRead = true;
            m.RepliedDate = DateTime.Now;
            m.RepliedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Admin";
            m.ReplyMessage = replyBody;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reply sent successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── MANUAL READ / UNREAD TOGGLE ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRead(int id)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();

            m.IsRead = !m.IsRead;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ── MANUAL "MARK AS REPLIED" (no email — e.g. replied by phone) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReplied(int id)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();

            m.IsReplied = true;
            m.IsRead = true;
            m.RepliedDate = DateTime.Now;
            m.RepliedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Admin";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Marked as replied.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();

            m.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Index));
        }

        private static string BuildReplyEmailBody(
            string originalName, string originalSubject, string originalMessage,
            string replyBody, string? collegeName)
        {
            var signOff = string.IsNullOrWhiteSpace(collegeName) ? "" : $"<p>Regards,<br/>{WebUtility.HtmlEncode(collegeName)}</p>";

            return $@"
                <p>Dear {WebUtility.HtmlEncode(originalName)},</p>
                <div>{WebUtility.HtmlEncode(replyBody).Replace("\n", "<br/>")}</div>
                {signOff}
                <hr/>
                <p style='color:#888;font-size:12px;'>
                    In reply to your message — <b>{WebUtility.HtmlEncode(originalSubject)}</b>:<br/>
                    <em>{WebUtility.HtmlEncode(originalMessage).Replace("\n", "<br/>")}</em>
                </p>";
        }
    }
}
