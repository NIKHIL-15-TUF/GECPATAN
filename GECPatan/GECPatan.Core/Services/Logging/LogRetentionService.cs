using GECPatan.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Logging
{
    /// <summary>
    /// Once a day deletes old ApplicationLogs and SecurityLogs. AuditLogs are never touched.
    /// Register it in ONE app only (Admin) - see AddApplicationLogging(runRetention: true).
    /// </summary>
    public sealed class LogRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ApplicationLogOptions _options;
        private readonly ILogger<LogRetentionService> _logger;

        public LogRetentionService(IServiceScopeFactory scopes, ApplicationLogOptions options, ILogger<LogRetentionService> logger)
        {
            _scopes = scopes; _options = options; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), ct);     // let startup/migrations finish first
                while (!ct.IsCancellationRequested)
                {
                    await PurgeAsync(ct);
                    await Task.Delay(TimeSpan.FromHours(24), ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task PurgeAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (_options.RetentionDays > 0)
                {
                    var cutoff = DateTime.Now.AddDays(-_options.RetentionDays);
                    var n = await db.ApplicationLogs.Where(l => l.Timestamp < cutoff).ExecuteDeleteAsync(ct);
                    if (n > 0) _logger.LogInformation("Retention: removed {Count} application logs older than {Days} days", n, _options.RetentionDays);
                }
                if (_options.SecurityRetentionDays > 0)
                {
                    var cutoff = DateTime.Now.AddDays(-_options.SecurityRetentionDays);
                    var n = await db.SecurityLogs.Where(l => l.Timestamp < cutoff).ExecuteDeleteAsync(ct);
                    if (n > 0) _logger.LogInformation("Retention: removed {Count} security logs older than {Days} days", n, _options.SecurityRetentionDays);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Log retention job failed");
            }
        }
    }
}
