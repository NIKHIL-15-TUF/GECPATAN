namespace GECPatan.Admin.Models.ViewModels
{
    public class ApplicationLogListVM
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ExceptionType { get; set; }
        public string? Exception { get; set; }
        public string? RequestPath { get; set; }
        public string? HttpMethod { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? TraceId { get; set; }
    }

    public class ApplicationLogFilterVM
    {
        public string? Search { get; set; }       // message, category, exception type, path or trace id
        public string? Level { get; set; }
        public string? Source { get; set; }
        public bool OnlyWithException { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;

        public List<ApplicationLogListVM> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 50;

        // last 24 hours
        public int Critical24h { get; set; }
        public int Errors24h { get; set; }
        public int Warnings24h { get; set; }
        public int Api24h { get; set; }
    }
}
