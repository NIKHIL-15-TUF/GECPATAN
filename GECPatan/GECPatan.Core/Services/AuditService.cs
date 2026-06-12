using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace GECPatan.Core.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }

        public async Task LogAsync(string action, string module,
            int? recordId = null, string? recordName = null,
            string? details = null)
        {
            var user = _httpContext.HttpContext?.User;
            if (user == null) return;

            var log = new AuditLog
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = user.Identity?.Name ?? "Unknown",
                UserRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "",
                Action = action,
                Module = module,
                RecordId = recordId,
                RecordName = recordName,
                Details = details,
                IpAddress = _httpContext.HttpContext?
                    .Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}