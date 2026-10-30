using System.Threading.Channels;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Logging
{
    /// <summary>Hand-off between loggers (any thread) and the single background writer.</summary>
    public class ApplicationLogQueue
    {
        private readonly Channel<ApplicationLog> _channel;

        public ApplicationLogQueue(ApplicationLogOptions options)
        {
            _channel = Channel.CreateBounded<ApplicationLog>(new BoundedChannelOptions(Math.Max(100, options.QueueCapacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,   // never block a request because the DB is slow
                SingleReader = true
            });
        }

        public bool TryEnqueue(ApplicationLog log) => _channel.Writer.TryWrite(log);
        public ChannelReader<ApplicationLog> Reader => _channel.Reader;
    }

    /// <summary>
    /// While true on the current async flow, nothing is queued. The writer sets it around its own
    /// SaveChanges so EF's own log lines (e.g. "database unavailable") can't feed back into the queue forever.
    /// </summary>
    internal static class LogSuppression
    {
        private static readonly AsyncLocal<bool> Flag = new();
        public static bool IsSuppressed => Flag.Value;
        public static IDisposable Begin() { Flag.Value = true; return new Scope(); }
        private sealed class Scope : IDisposable { public void Dispose() => Flag.Value = false; }
    }

    [ProviderAlias("Database")]   // lets you tune it with "Logging": { "Database": { "LogLevel": {...} } }
    public sealed class DbLoggerProvider : ILoggerProvider
    {
        private readonly ApplicationLogOptions _options;
        private readonly ApplicationLogQueue _queue;
        private readonly IHttpContextAccessor _http;

        public DbLoggerProvider(ApplicationLogOptions options, ApplicationLogQueue queue, IHttpContextAccessor http)
        {
            _options = options; _queue = queue; _http = http;
        }

        public ILogger CreateLogger(string categoryName) => new DbLogger(categoryName, _options, _queue, _http);
        public void Dispose() { }
    }

    internal sealed class DbLogger : ILogger
    {
        private readonly string _category;
        private readonly ApplicationLogOptions _options;
        private readonly ApplicationLogQueue _queue;
        private readonly IHttpContextAccessor _http;
        private readonly bool _excluded;

        public DbLogger(string category, ApplicationLogOptions options, ApplicationLogQueue queue, IHttpContextAccessor http)
        {
            _category = category; _options = options; _queue = queue; _http = http;
            _excluded = options.ExcludedCategories.Any(p => category.StartsWith(p, StringComparison.Ordinal));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel level) =>
            !_excluded && level != LogLevel.None && level >= _options.MinimumLevel && !LogSuppression.IsSuppressed;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;

            try
            {
                var ctx = _http.HttpContext;
                var user = ctx?.User;

                _queue.TryEnqueue(new ApplicationLog
                {
                    Timestamp = DateTime.Now,
                    Level = level.ToString(),
                    Source = _options.Source,
                    Category = Trim(_category, 300)!,
                    Message = Trim(Flat(formatter(state, exception)), 4000) ?? "",
                    ExceptionType = Trim(exception?.GetType().FullName, 300),
                    Exception = Trim(exception?.ToString(), 20000),
                    RequestPath = Trim(ctx?.Request.Path.Value, 300),
                    HttpMethod = Trim(ctx?.Request.Method, 10),
                    UserName = Trim(user?.Identity?.IsAuthenticated == true ? user.Identity.Name : null, 256),
                    IpAddress = Trim(ctx?.Connection.RemoteIpAddress?.ToString(), 50),
                    TraceId = Trim(ctx?.TraceIdentifier, 100)
                });
            }
            catch
            {
                // A logger must never throw into application code.
            }
        }

        private static string Flat(string s) => s.Replace('\r', ' ').Replace('\n', ' ');
        private static string? Trim(string? s, int max) =>
            string.IsNullOrEmpty(s) ? null : (s.Length <= max ? s : s[..max]);

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();
            public void Dispose() { }
        }
    }
}
