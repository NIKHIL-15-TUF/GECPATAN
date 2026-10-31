using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.Audit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GECPatan.Core.Services
{
    /// <summary>
    /// Create / Update / Delete of database records is audited AUTOMATICALLY
    /// (see AuditSaveChangesInterceptor) - do not call this for those.
    /// Use it only for events that are not a data change, e.g.
    ///   await _audit.LogAsync("Exported", "Faculty", recordName: "Faculty list (Excel)");
    ///   await _audit.LogAsync("Downloaded", "Document", id, doc.Title);
    /// </summary>
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly AuditOptions _options;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContext, AuditOptions options)
        {
            _context = context;
            _httpContext = httpContext;
            _options = options;
        }

        public async Task LogAsync(string action, string module,
            int? recordId = null, string? recordName = null,
            string? details = null)
        {
            var http = _httpContext.HttpContext;
            var user = http?.User;
            if (http == null || user?.Identity?.IsAuthenticated != true) return;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = user.Identity.Name ?? "Unknown",
                UserRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "",
                Action = action,
                Module = module,
                RecordId = recordId,
                RecordName = recordName,
                Details = details,
                IpAddress = http.Connection.RemoteIpAddress?.ToString(),
                Source = _options.Source,
                RequestPath = $"{http.Request.Method} {http.Request.Path}",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}
