namespace GECPatan.Core.Services.Email
{
    /// <summary>
    /// Single, centralized entry point for sending outbound email — used by
    /// the Api's Contact Us submission handler (notifies the configured
    /// support address) and the Admin Portal's Contact Message reply feature
    /// (emails the person who submitted the form).
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an HTML email. Never throws — returns false and logs on any
        /// failure, since a failed notification email should not block the
        /// caller's primary operation (e.g. saving a Contact Message).
        /// </summary>
        /// <param name="toEmail">Recipient address.</param>
        /// <param name="subject">Email subject line.</param>
        /// <param name="htmlBody">HTML email body.</param>
        /// <param name="fromEmail">
        /// Optional override for the "From" address (e.g. the configured
        /// support/sender email). When null, falls back to the SMTP username
        /// configured in EmailOptions.
        /// </param>
        /// <param name="fromDisplayName">Optional "From" display name.</param>
        /// <param name="replyToEmail">Optional Reply-To address.</param>
        Task<bool> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string? fromEmail = null,
            string? fromDisplayName = null,
            string? replyToEmail = null,
            CancellationToken ct = default);
    }
}
