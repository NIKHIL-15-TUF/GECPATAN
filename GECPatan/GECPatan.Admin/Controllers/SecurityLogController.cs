using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    // Security logs reveal IPs and attack attempts -> SuperAdmin only.
    // Read-only on purpose: there is no edit or delete action.
    [Authorize(Roles = "SuperAdmin")]
    public class SecurityLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SecurityLogController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(SecurityLogFilterVM filter)
        {
            ViewData["Title"] = "Security Logs";
            if (filter.Page < 1) filter.Page = 1;

            // Summary tiles (one grouped query)
            var since = DateTime.Now.AddHours(-24);
            var counts = await _context.SecurityLogs
                .Where(l => l.Timestamp >= since)
                .GroupBy(l => l.EventType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
            int C(string t) => counts.TryGetValue(t, out var n) ? n : 0;
            filter.FailedLogins24h = C(SecurityEvents.LoginFailed) + C(SecurityEvents.LoginBlockedInactive);
            filter.Lockouts24h = C(SecurityEvents.AccountLockedOut);
            filter.AccessDenied24h = C(SecurityEvents.AccessDenied) + C(SecurityEvents.UnauthorizedAccess);
            filter.RateLimited24h = C(SecurityEvents.RateLimitExceeded);

            // Filters
            var query = _context.SecurityLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(l =>
                    (l.UserName != null && l.UserName.Contains(s)) ||
                    (l.AttemptedEmail != null && l.AttemptedEmail.Contains(s)) ||
                    (l.IpAddress != null && l.IpAddress.Contains(s)));
            }
            if (!string.IsNullOrEmpty(filter.EventType)) query = query.Where(l => l.EventType == filter.EventType);
            if (!string.IsNullOrEmpty(filter.Severity)) query = query.Where(l => l.Severity == filter.Severity);
            if (!string.IsNullOrEmpty(filter.Source)) query = query.Where(l => l.Source == filter.Source);
            if (filter.DateFrom.HasValue) query = query.Where(l => l.Timestamp >= filter.DateFrom.Value.Date);
            if (filter.DateTo.HasValue) query = query.Where(l => l.Timestamp < filter.DateTo.Value.Date.AddDays(1));

            filter.TotalCount = await query.CountAsync();
            filter.TotalPages = Math.Max(1, (int)Math.Ceiling(filter.TotalCount / (double)filter.PageSize));
            if (filter.Page > filter.TotalPages) filter.Page = filter.TotalPages;

            var rows = await query
                .OrderByDescending(l => l.Timestamp).ThenByDescending(l => l.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Format dates in memory (ToString with a format can't be translated to SQL)
            filter.Logs = rows.Select(l => new SecurityLogListVM
            {
                Id = l.Id,
                Timestamp = l.Timestamp.ToString("dd MMM yyyy, hh:mm:ss tt"),
                EventType = l.EventType,
                Severity = l.Severity,
                Source = l.Source,
                UserName = l.UserName,
                AttemptedEmail = l.AttemptedEmail,
                IpAddress = l.IpAddress,
                RequestPath = l.RequestPath,
                StatusCode = l.StatusCode,
                UserAgent = l.UserAgent,
                Details = l.Details
            }).ToList();

            return View(filter);
        }
    }
}
