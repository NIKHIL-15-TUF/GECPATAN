using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace GECPatan.Core.Services.Email
{
    /// <summary>
    /// Default IEmailService implementation, built on the BCL's
    /// System.Net.Mail.SmtpClient — deliberately no extra NuGet dependency,
    /// consistent with keeping Core's dependency footprint minimal.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailOptions options, ILogger<EmailService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<bool> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string? fromEmail = null,
            string? fromDisplayName = null,
            string? replyToEmail = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendAsync called with an empty recipient address; subject: {Subject}", subject);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                _logger.LogWarning("Smtp:Host is not configured — skipping email send (subject: {Subject}).", subject);
                return false;
            }

            var effectiveFrom = string.IsNullOrWhiteSpace(fromEmail) ? _options.Username : fromEmail;

            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Timeout = _options.TimeoutMs,
                    Credentials = string.IsNullOrWhiteSpace(_options.Username)
                        ? null
                        : new NetworkCredential(_options.Username, _options.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(effectiveFrom, fromDisplayName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(replyToEmail))
                    message.ReplyToList.Add(new MailAddress(replyToEmail));

                await client.SendMailAsync(message, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email to {ToEmail} with subject '{Subject}'.", toEmail, subject);
                return false;
            }
        }
    }
}
