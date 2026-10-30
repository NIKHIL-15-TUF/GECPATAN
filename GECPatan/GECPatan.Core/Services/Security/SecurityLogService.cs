using System.Collections.Concurrent;
using System.Security.Claims;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Security
{
    public interface ISecurityLogService
    {
        /// <summary>
        /// Writes one security event. Never throws - a logging failure must not break a login
        /// or an API call. User / IP / path are taken from <paramref name="http"/> when not given.
        /// </summary>
        /// <param name="throttleKey">
        /// When set, the same key is written at most once per throttle window (default 30 s).
        /// Use it for events anyone on the internet can trigger (401/403/429) so they can't flood the table.
        /// </param>
        Task LogAsync(HttpContext? http, string eventType, string severity = SecuritySeverity.Info,
            string? details = null, string? userId = null, string? userName = null,
            string? attemptedEmail = null, int? statusCode = null, string? throttleKey = null);
    }

    /// <summary>
    /// Singleton. Each write uses its own short-lived DbContext scope, so it never saves (or
    /// rolls back) whatever the calling controller has pending in its own context.
    /// </summary>
    public class SecurityLogService : ISecurityLogService
    {
        private static readonly TimeSpan ThrottleWindow = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SecurityLogService> _logger;
        private readonly string _source;
        private readonly ConcurrentDictionary<string, DateTime> _recent = new();

        public SecurityLogService(IServiceScopeFactory scopes, ILogger<SecurityLogService> logger, string source)
        {
            _scopes = scopes;
            _logger = logger;
            _source = source;
        }

        public async Task LogAsync(HttpContext? http, string eventType, string severity = SecuritySeverity.Info,
            string? details = null, string? userId = null, string? userName = null,
            string? attemptedEmail = null, int? statusCode = null, string? throttleKey = null)
        {
            try
            {
                if (throttleKey != null && IsThrottled(throttleKey)) return;

                var user = http?.User;
                var entry = new SecurityLog
                {
                    Timestamp = DateTime.Now,
                    EventType = Clean(eventType, 50)!,
                    Severity = Clean(severity, 20)!,
                    Source = _source,
                    UserId = Clean(userId ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, 450),
                    UserName = Clean(userName ?? (user?.Identity?.IsAuthenticated == true ? user.Identity.Name : null), 256),
                    AttemptedEmail = Clean(attemptedEmail, 256),
                    IpAddress = Clean(http?.Connection.RemoteIpAddress?.ToString(), 50),
                    UserAgent = Clean(http?.Request.Headers["User-Agent"].ToString(), 300),
                    RequestPath = Clean(http?.Request.Path.Value, 300),
                    HttpMethod = Clean(http?.Request.Method, 10),
                    StatusCode = statusCode,
                    Details = Clean(details, 1000)
                };

                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.SecurityLogs.Add(entry);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not write security log {EventType}", eventType);
            }
        }

        private bool IsThrottled(string key)
        {
            var now = DateTime.UtcNow;
            if (_recent.TryGetValue(key, out var last) && now - last < ThrottleWindow) return true;
            _recent[key] = now;

            if (_recent.Count > 5000)   // keep memory bounded under a flood
                foreach (var k in _recent.Where(p => now - p.Value >= ThrottleWindow).Select(p => p.Key).ToList())
                    _recent.TryRemove(k, out _);
            return false;
        }

        // Strips line breaks (log injection) and trims to the column length.
        private static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var s = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return s.Length <= max ? s : s[..max];
        }
    }

    public static class SecurityLogServiceCollectionExtensions
    {
        /// <param name="source">"Admin" or "Api" - stored on every row.</param>
        public static IServiceCollection AddSecurityLogging(this IServiceCollection services, string source)
        {
            services.AddSingleton<ISecurityLogService>(sp => new SecurityLogService(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<SecurityLogService>>(),
                source));
            return services;
        }
    }
}
