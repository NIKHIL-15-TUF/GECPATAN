namespace GECPatan.Core.Services.Email
{
    /// <summary>
    /// Bound from the "Smtp" section of appsettings.json in whichever project
    /// registers IEmailService (GECPatan.Api and GECPatan.Admin both do — see
    /// their respective Program.cs). These are transport credentials, so they
    /// live in appsettings (ideally overridden via environment variables /
    /// user-secrets / Key Vault in real deployments), never in the database —
    /// unlike the "From"/"To" addresses actually used for Contact Us mail,
    /// which are admin-editable SiteSettings (see ContactController).
    /// </summary>
    public class EmailOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        /// <summary>SMTP auth username — for most providers this must match, or be authorized to send as, the "From" address.</summary>
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>Timeout in milliseconds for the underlying SmtpClient.</summary>
        public int TimeoutMs { get; set; } = 15000;
    }
}
