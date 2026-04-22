using GECPatan.Admin.Data;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(AuditLogFilterVM filter)
        {
            ViewData["Title"] = "Audit Logs";

            // Build available filter options
            filter.Modules = await _context.AuditLogs
                .Where(a => a.Module != null)
                .Select(a => a.Module!)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            filter.Actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Query with filters
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserName))
                query = query.Where(a => a.UserName != null &&
                                    a.UserName.Contains(filter.UserName));

            if (!string.IsNullOrEmpty(filter.Module))
                query = query.Where(a => a.Module == filter.Module);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(a => a.Timestamp <=
                    filter.DateTo.Value.AddDays(1));

            filter.Logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(500) // Show max 500 at a time
                .Select(a => new AuditLogListVM
                {
                    Id = a.Id,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    Module = a.Module,
                    RecordName = a.RecordName,
                    Timestamp = a.Timestamp.ToString("dd MMM yyyy, hh:mm tt"),
                    IpAddress = a.IpAddress,
                    Details = a.Details
                })
                .ToListAsync();

            return View(filter);
        }
    }
}
