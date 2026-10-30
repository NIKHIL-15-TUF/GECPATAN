using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    /// <summary>
    /// One security-relevant event (failed login, lockout, access denied, rate limit hit...).
    /// Written by both GECPatan.Admin and GECPatan.Api into the same table.
    /// Never contains passwords or tokens.
    /// </summary>
    public class SecurityLog
    {
        public long Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>See <see cref="SecurityEvents"/>.</summary>
        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>Info / Warning / Critical - see <see cref="SecuritySeverity"/>.</summary>
        [Required, MaxLength(20)]
        public string Severity { get; set; } = SecuritySeverity.Info;

        /// <summary>"Admin" or "Api" - which app wrote the row.</summary>
        [Required, MaxLength(20)]
        public string Source { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        /// <summary>E-mail typed on the login form (for failed logins where no user is signed in).</summary>
        [MaxLength(256)]
        public string? AttemptedEmail { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        [MaxLength(300)]
        public string? RequestPath { get; set; }

        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        public int? StatusCode { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }
    }

    public static class SecuritySeverity
    {
        public const string Info = "Info";
        public const string Warning = "Warning";
        public const string Critical = "Critical";
        public static readonly string[] All = { Info, Warning, Critical };
    }

    public static class SecurityEvents
    {
        // Authentication (Admin)
        public const string LoginSuccess = "LoginSuccess";
        public const string LoginFailed = "LoginFailed";
        public const string LoginBlockedInactive = "LoginBlockedInactive";
        public const string AccountLockedOut = "AccountLockedOut";
        public const string Logout = "Logout";
        public const string PasswordChanged = "PasswordChanged";
        public const string PasswordChangeFailed = "PasswordChangeFailed";

        // Admin-performed account actions
        public const string PasswordResetByAdmin = "PasswordResetByAdmin";
        public const string AccountActivated = "AccountActivated";
        public const string AccountDeactivated = "AccountDeactivated";

        // Access control (Admin + Api)
        public const string AccessDenied = "AccessDenied";           // 403
        public const string UnauthorizedAccess = "UnauthorizedAccess"; // 401
        public const string RateLimitExceeded = "RateLimitExceeded";   // 429

        public static readonly string[] All =
        {
            LoginSuccess, LoginFailed, LoginBlockedInactive, AccountLockedOut, Logout,
            PasswordChanged, PasswordChangeFailed, PasswordResetByAdmin,
            AccountActivated, AccountDeactivated,
            AccessDenied, UnauthorizedAccess, RateLimitExceeded
        };
    }
}
