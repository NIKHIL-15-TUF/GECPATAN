using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GECPatan.Core.Services.Logging
{
    /// <summary>
    /// Background writer: drains the queue and inserts logs in batches, so logging never slows a request.
    /// Deliberately uses Console.Error (not ILogger) for its own failures - logging a logging failure
    /// through the same pipeline would loop.
    /// </summary>
    public sealed class ApplicationLogWriter : BackgroundService
    {
        private const int MaxBatch = 100;
        private readonly ApplicationLogQueue _queue;
        private readonly IServiceScopeFactory _scopes;

        public ApplicationLogWriter(ApplicationLogQueue queue, IServiceScopeFactory scopes)
        {
            _queue = queue; _scopes = scopes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<ApplicationLog>(MaxBatch);
            try
            {
                while (await _queue.Reader.WaitToReadAsync(stoppingToken))
                {
                    Drain(batch);
                    await SaveAsync(batch);
                    batch.Clear();
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);   // coalesce bursts
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }

            // Flush whatever is left on shutdown
            Drain(batch);
            await SaveAsync(batch);
        }

        private void Drain(List<ApplicationLog> batch)
        {
            while (batch.Count < MaxBatch && _queue.Reader.TryRead(out var item)) batch.Add(item);
        }

        private async Task SaveAsync(List<ApplicationLog> batch)
        {
            if (batch.Count == 0) return;
            try
            {
                using var _ = LogSuppression.Begin();
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ApplicationLogs.AddRange(batch);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // DB down, table missing (migration not applied), etc. Drop this batch rather than retry forever.
                Console.Error.WriteLine($"[ApplicationLogWriter] Dropped {batch.Count} log line(s): {ex.GetBaseException().Message}");
            }
        }
    }
}
