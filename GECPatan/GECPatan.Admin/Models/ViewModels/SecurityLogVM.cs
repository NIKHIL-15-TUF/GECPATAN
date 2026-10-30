namespace GECPatan.Admin.Models.ViewModels
{
    public class SecurityLogListVM
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? AttemptedEmail { get; set; }
        public string? IpAddress { get; set; }
        public string? RequestPath { get; set; }
        public int? StatusCode { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
    }

    public class SecurityLogFilterVM
    {
        public string? Search { get; set; }      // user name, e-mail or IP
        public string? EventType { get; set; }
        public string? Severity { get; set; }
        public string? Source { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;

        // results
        public List<SecurityLogListVM> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 50;

        // last-24-hours summary tiles
        public int FailedLogins24h { get; set; }
        public int Lockouts24h { get; set; }
        public int AccessDenied24h { get; set; }
        public int RateLimited24h { get; set; }
    }
}
