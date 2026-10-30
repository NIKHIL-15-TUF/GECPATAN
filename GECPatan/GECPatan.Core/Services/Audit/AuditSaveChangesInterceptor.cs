using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Audit
{
    public sealed class AuditOptions
    {
        /// <summary>"Admin" or "Api" - stored on every audit row.</summary>
        public string Source { get; set; } = "Admin";
    }

    /// <summary>
    /// Records every Create / Update / Delete made by a signed-in user, for ALL
    /// entities, with the old and new values - no per-controller code needed.
    ///
    ///  * Updates and deletes are written in the SAME transaction as the change
    ///    itself (so a failed save never leaves a false audit row).
    ///  * Creates are written immediately after the save (through a separate
    ///    scope), because the new record's Id does not exist until then.
    ///  * Anonymous requests, seeders and background jobs are not audited.
    ///  * Audit rows are append-only: editing or deleting one throws.
    ///  * An error inside the audit code is logged and never blocks the save.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        // Housekeeping columns - never reported as a "change"
        private static readonly HashSet<string> NoiseProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedDate", "CreatedDateInt", "CreatedBy",
            "UpdatedDate", "UpdatedDateInt", "UpdatedBy",
            "ConcurrencyStamp", "SecurityStamp", "PasswordHash",
            "AccessFailedCount", "LockoutEnd", "NormalizedUserName", "NormalizedEmail"
        };

        // Never written into the log, not even in a "created" snapshot
        private static readonly HashSet<string> SecretProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "SecurityStamp", "ConcurrencyStamp"
        };

        // Which property names make a good human label for a record, best first
        private static readonly string[] NameProps =
        {
            "Name", "Title", "FullName", "Heading", "Subject", "FileName",
            "UserName", "Email", "Label", "Key", "Text"
        };

        private static readonly Regex Words = new(
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

        private const int MaxValueLength = 200;
        private const int MaxFields = 40;
        private const int MaxDetailsLength = 16000;

        private readonly IHttpContextAccessor _http;
        private readonly IServiceScopeFactory _scopes;
        private readonly AuditOptions _options;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;

        // Creates waiting for their generated Id (per DbContext instance)
        private readonly ConcurrentDictionary<Guid, List<(EntityEntry Entry, AuditLog Log)>> _pending = new();

        public AuditSaveChangesInterceptor(IHttpContextAccessor http, IServiceScopeFactory scopes,
            AuditOptions options, ILogger<AuditSaveChangesInterceptor> logger)
        {
            _http = http; _scopes = scopes; _options = options; _logger = logger;
        }

        // ══════════ EF Core hooks ══════════

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Collect(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            Collect(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            var logs = TakePending(eventData.Context);
            if (logs != null)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.AuditLogs.AddRange(logs);
                    db.SaveChanges();
                }
                catch (Exception ex) { _logger.LogError(ex, "Audit: could not save 'Created' audit rows"); }
            }
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            var logs = TakePending(eventData.Context);
            if (logs != null)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.AuditLogs.AddRange(logs);
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex) { _logger.LogError(ex, "Audit: could not save 'Created' audit rows"); }
            }
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
            => Forget(eventData.Context);

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
        {
            Forget(eventData.Context);
            return Task.CompletedTask;
        }

        // ══════════ Core logic ══════════

        private void Forget(DbContext? ctx)
        {
            if (ctx != null) _pending.TryRemove(ctx.ContextId.InstanceId, out _);
        }

        // Fill in the generated Ids of newly created records.
        private List<AuditLog>? TakePending(DbContext? ctx)
        {
            if (ctx == null || !_pending.TryRemove(ctx.ContextId.InstanceId, out var list) || list.Count == 0)
                return null;

            foreach (var (entry, log) in list)
            {
                try { log.RecordId = GetIntKey(entry); }
                catch { /* leave null */ }
                if (string.IsNullOrEmpty(log.RecordName))
                    log.RecordName = log.RecordId.HasValue ? $"{log.Module} #{log.RecordId}" : log.Module;
            }
            return list.Select(x => x.Log).ToList();
        }

        private void Collect(DbContext? ctx)
        {
            if (ctx == null) return;

            try
            {
                // Entries() runs change detection, so states are up to date.
                var entries = ctx.ChangeTracker.Entries().ToList();

                // Append-only: nobody may alter or remove existing audit rows.
                if (entries.Any(e => e.Entity is AuditLog &&
                                     e.State is EntityState.Modified or EntityState.Deleted))
                    throw new InvalidOperationException("Audit logs are append-only and cannot be modified or deleted.");

                var http = _http.HttpContext;
                var principal = http?.User;
                if (principal?.Identity?.IsAuthenticated != true) return;   // anonymous / background work

                var who = principal.Identity!.Name ?? "Unknown";
                var pendingCreates = new List<(EntityEntry, AuditLog)>();

                foreach (var entry in entries)
                {
                    if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
                    if (!ShouldAudit(entry.Metadata.ClrType)) continue;

                    // Record who touched the row (the BaseEntity columns existed but were never filled)
                    if (entry.Entity is BaseEntity be)
                    {
                        if (entry.State == EntityState.Added) be.CreatedBy ??= who;
                        else if (entry.State == EntityState.Modified) be.UpdatedBy = who;
                    }

                    var log = BuildLog(ctx, entry, http!, principal);
                    if (log == null) continue;

                    if (entry.State == EntityState.Added)
                        pendingCreates.Add((entry, log));      // needs the generated Id -> after save
                    else
                        ctx.Set<AuditLog>().Add(log);          // same transaction as the change
                }

                if (pendingCreates.Count > 0)
                    _pending[ctx.ContextId.InstanceId] = pendingCreates;
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("Audit logs are append-only"))
            {
                throw;
            }
            catch (Exception ex)
            {
                // Auditing must never stop a real save.
                _logger.LogError(ex, "Audit: failed to collect changes");
            }
        }

        private static bool ShouldAudit(Type t)
        {
            if (t == typeof(AuditLog) || t == typeof(SecurityLog) || t == typeof(ApplicationLog)
                || t == typeof(Notification) || t == typeof(NotificationRead))
                return false;

            // Identity plumbing (tokens, claims, logins ...) is noise; keep role assignments.
            if (t.Namespace == "Microsoft.AspNetCore.Identity")
                return t == typeof(IdentityUserRole<string>);

            return true;
        }

        private AuditLog? BuildLog(DbContext ctx, EntityEntry entry, HttpContext http, ClaimsPrincipal user)
        {
            var type = entry.Metadata.ClrType;
            string action;
            string? details;
            string? recordName;

            if (entry.Entity is IdentityUserRole<string> ur)
            {
                // "ravi -> HOD"
                var userName = ctx.Set<ApplicationUser>().AsNoTracking()
                    .Where(u => u.Id == ur.UserId).Select(u => u.UserName).FirstOrDefault() ?? ur.UserId;
                var roleName = ctx.Set<IdentityRole>().AsNoTracking()
                    .Where(r => r.Id == ur.RoleId).Select(r => r.Name).FirstOrDefault() ?? ur.RoleId;
                action = entry.State == EntityState.Added ? "Role Assigned" : "Role Removed";
                recordName = Trim($"{userName} → {roleName}", 300);
                details = null;
                return Make(user, http, action, "User Role", null, recordName, details);
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    details = Json(new { values = Snapshot(entry, useOriginal: false) });
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    details = Json(new { values = Snapshot(entry, useOriginal: true) });
                    break;

                default: // Modified
                    var soft = SoftDeleteChange(entry);
                    if (soft != null)
                    {
                        action = soft;
                        details = Json(new { values = Snapshot(entry, useOriginal: false) });
                        break;
                    }

                    var changes = Diff(entry, out var onlyNoise);
                    if (onlyNoise) return null;                    // only housekeeping columns changed
                    action = "Updated";
                    details = changes.Count > 0 ? Json(new { changes }) : null;
                    break;
            }

            var recordId = entry.State == EntityState.Added ? null : GetIntKey(entry);
            recordName = RecordName(entry)
                         ?? (recordId.HasValue ? $"{FriendlyName(type)} #{recordId}" : null);
            return Make(user, http, action, FriendlyName(type), recordId, recordName, details);
        }

        private AuditLog Make(ClaimsPrincipal user, HttpContext http, string action, string module,
            int? recordId, string? recordName, string? details)
            => new()
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = user.Identity?.Name ?? "Unknown",
                UserRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "",
                Action = action,
                Module = module,
                RecordId = recordId,
                RecordName = Trim(recordName, 300),
                Details = details,
                IpAddress = Trim(http.Connection.RemoteIpAddress?.ToString(), 50),
                Source = _options.Source,
                RequestPath = Trim($"{http.Request.Method} {http.Request.Path}", 300),
                Timestamp = DateTime.Now
            };

        // ── helpers ─────────────────────────────────────────

        // BaseEntity soft delete: IsDeleted false->true = "Deleted", true->false = "Restored"
        private static string? SoftDeleteChange(EntityEntry entry)
        {
            if (entry.Entity is not BaseEntity) return null;
            var p = entry.Property(nameof(BaseEntity.IsDeleted));
            if (!p.IsModified) return null;
            var was = p.OriginalValue is true;
            var now = p.CurrentValue is true;
            if (!was && now) return "Deleted";
            if (was && !now) return "Restored";
            return null;
        }

        private static List<object> Diff(EntityEntry entry, out bool onlyNoise)
        {
            var result = new List<object>();
            var modified = entry.Properties.Where(p => p.IsModified).ToList();
            var meaningful = modified.Where(p => !NoiseProps.Contains(p.Metadata.Name)
                                                 && !p.Metadata.IsPrimaryKey()).ToList();
            onlyNoise = meaningful.Count == 0;

            foreach (var p in meaningful)
            {
                if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                if (result.Count >= MaxFields) break;
                result.Add(new
                {
                    field = p.Metadata.Name,
                    old = Display(p.OriginalValue),
                    @new = Display(p.CurrentValue)
                });
            }
            return result;
        }

        private static Dictionary<string, string?> Snapshot(EntityEntry entry, bool useOriginal)
        {
            var values = new Dictionary<string, string?>();
            foreach (var p in entry.Properties)
            {
                var name = p.Metadata.Name;
                if (SecretProps.Contains(name) || p.Metadata.IsPrimaryKey() && entry.State == EntityState.Added) continue;
                if (name is "IsDeleted" or "CreatedDate" or "CreatedDateInt" or "UpdatedDate" or "UpdatedDateInt"
                    or "CreatedBy" or "UpdatedBy") continue;

                var raw = useOriginal ? p.OriginalValue : p.CurrentValue;
                if (raw == null || raw is byte[]) continue;
                if (raw is string s && s.Length == 0) continue;

                values[name] = Display(raw);
                if (values.Count >= MaxFields) break;
            }
            return values;
        }

        private static string? Display(object? v) => v switch
        {
            null => null,
            byte[] => "(binary)",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
            bool b => b ? "Yes" : "No",
            _ => Trim(v.ToString(), MaxValueLength)
        };

        private static string? RecordName(EntityEntry entry)
        {
            foreach (var n in NameProps)
            {
                var prop = entry.Properties.FirstOrDefault(p =>
                    string.Equals(p.Metadata.Name, n, StringComparison.OrdinalIgnoreCase));
                var val = prop?.CurrentValue as string;
                if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            }
            return null;   // filled in as "<Module> #<id>" later
        }

        private static int? GetIntKey(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null || key.Properties.Count != 1) return null;
            var v = entry.Property(key.Properties[0].Name).CurrentValue;
            return v is int i && i > 0 ? i : null;
        }

        private static readonly Dictionary<string, string> NameOverrides = new()
        {
            ["MoU"] = "MoU", ["ApplicationUser"] = "User"
        };

        private static string FriendlyName(Type t)
            => NameOverrides.TryGetValue(t.Name, out var o) ? o : Words.Replace(t.Name, " ");

        private static string? Trim(string? s, int max)
            => s == null ? null : s.Length <= max ? s : s[..(max - 1)] + "…";

        private static string? Json(object o)
        {
            var s = JsonSerializer.Serialize(o);
            return s.Length <= MaxDetailsLength ? s : "{\"note\":\"Details too large to store\"}";
        }
    }
}
