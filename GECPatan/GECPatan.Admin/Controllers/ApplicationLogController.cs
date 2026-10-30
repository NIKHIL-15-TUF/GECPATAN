using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    // Stack traces and paths are sensitive -> SuperAdmin only, read-only.
    [Authorize(Roles = "SuperAdmin")]
    public class ApplicationLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ApplicationLogController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(ApplicationLogFilterVM filter)
        {
            ViewData["Title"] = "Application Logs";
            if (filter.Page < 1) filter.Page = 1;

            // Summary tiles
            var since = DateTime.Now.AddHours(-24);
            var recent = _context.ApplicationLogs.Where(l => l.Timestamp >= since);
            var byLevel = await recent.GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Level, x => x.Count);
            int C(string k) => byLevel.TryGetValue(k, out var n) ? n : 0;
            filter.Critical24h = C("Critical");
            filter.Errors24h = C("Error");
            filter.Warnings24h = C("Warning");
            filter.Api24h = await recent.CountAsync(l => l.Source == "Api" && (l.Level == "Error" || l.Level == "Critical"));

            // Filters
            var query = _context.ApplicationLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(l =>
                    l.Message.Contains(s) || l.Category.Contains(s) ||
                    (l.ExceptionType != null && l.ExceptionType.Contains(s)) ||
                    (l.RequestPath != null && l.RequestPath.Contains(s)) ||
                    (l.TraceId != null && l.TraceId == s));
            }
            if (!string.IsNullOrEmpty(filter.Level)) query = query.Where(l => l.Level == filter.Level);
            if (!string.IsNullOrEmpty(filter.Source)) query = query.Where(l => l.Source == filter.Source);
            if (filter.OnlyWithException) query = query.Where(l => l.Exception != null);
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

            filter.Logs = rows.Select(l => new ApplicationLogListVM
            {
                Id = l.Id,
                Timestamp = l.Timestamp.ToString("dd MMM yyyy, hh:mm:ss tt"),
                Level = l.Level,
                Source = l.Source,
                Category = l.Category,
                Message = l.Message,
                ExceptionType = l.ExceptionType,
                Exception = l.Exception,
                RequestPath = l.RequestPath,
                HttpMethod = l.HttpMethod,
                UserName = l.UserName,
                IpAddress = l.IpAddress,
                TraceId = l.TraceId
            }).ToList();

            return View(filter);
        }
    }
}
