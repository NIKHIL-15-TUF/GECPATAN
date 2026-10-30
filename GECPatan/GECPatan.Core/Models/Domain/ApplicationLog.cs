using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    /// <summary>
    /// One application log line (warning / error / critical by default) written by
    /// GECPatan.Admin or GECPatan.Api through the database logger provider.
    /// </summary>
    public class ApplicationLog
    {
        public long Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Trace / Debug / Information / Warning / Error / Critical.</summary>
        [Required, MaxLength(20)]
        public string Level { get; set; } = "Error";

        /// <summary>"Admin" or "Api".</summary>
        [Required, MaxLength(20)]
        public string Source { get; set; } = string.Empty;

        /// <summary>ILogger category, usually the class name that logged it.</summary>
        [Required, MaxLength(300)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? ExceptionType { get; set; }

        /// <summary>Exception.ToString(): message, inner exceptions and stack trace.</summary>
        public string? Exception { get; set; }

        // Request context (null for background work)
        [MaxLength(300)]
        public string? RequestPath { get; set; }     // path only, never the query string

        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>HttpContext.TraceIdentifier - matches the "Reference" id returned to API clients.</summary>
        [MaxLength(100)]
        public string? TraceId { get; set; }
    }
}
