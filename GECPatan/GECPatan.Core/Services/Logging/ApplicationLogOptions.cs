using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Logging
{
    /// <summary>Bound from the "ApplicationLogging" section of appsettings.json.</summary>
    public class ApplicationLogOptions
    {
        /// <summary>Set by AddApplicationLogging: "Admin" or "Api".</summary>
        public string Source { get; set; } = "App";

        /// <summary>Lowest level stored. Warning keeps the table small; use Information while debugging.</summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Warning;

        /// <summary>Categories that are never stored (prefix match).</summary>
        public List<string> ExcludedCategories { get; set; } = new()
        {
            // Admin's ErrorController already logs the same exception with request details.
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"
        };

        /// <summary>Max log lines waiting to be written; extra lines are dropped (never blocks a request).</summary>
        public int QueueCapacity { get; set; } = 5000;

        /// <summary>ApplicationLogs older than this are deleted daily. 0 = keep forever.</summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>SecurityLogs older than this are deleted daily. 0 = keep forever.</summary>
        public int SecurityRetentionDays { get; set; } = 365;
    }
}
