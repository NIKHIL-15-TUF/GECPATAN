using System.Text.Json;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    // Read-only on purpose: there is no edit or delete action, and the audit
    // interceptor refuses to modify or remove existing audit rows.
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
            if (filter.Page < 1) filter.Page = 1;

            // Summary tiles (last 24 hours)
            var since = DateTime.Now.AddHours(-24);
            var recent = _context.AuditLogs.AsNoTracking().Where(a => a.Timestamp >= since);
            var counts = await recent.GroupBy(a => a.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
            int C(string a) => counts.TryGetValue(a, out var n) ? n : 0;
            filter.Created24h = C("Created");
            filter.Updated24h = C("Updated");
            filter.Deleted24h = C("Deleted");
            filter.ActiveUsers24h = await recent.Where(a => a.UserId != null)
                .Select(a => a.UserId).Distinct().CountAsync();

            // Dropdown options
            filter.Modules = await _context.AuditLogs.AsNoTracking()
                .Where(a => a.Module != null).Select(a => a.Module!)
                .Distinct().OrderBy(m => m).ToListAsync();
            filter.Actions = await _context.AuditLogs.AsNoTracking()
                .Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();

            // Filters (these were built before but never applied)
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(a =>
                    (a.UserName != null && a.UserName.Contains(s)) ||
                    (a.RecordName != null && a.RecordName.Contains(s)));
            }
            if (!string.IsNullOrEmpty(filter.Module)) query = query.Where(a => a.Module == filter.Module);
            if (!string.IsNullOrEmpty(filter.ActionType)) query = query.Where(a => a.Action == filter.ActionType);
            if (!string.IsNullOrEmpty(filter.Source)) query = query.Where(a => a.Source == filter.Source);
            if (filter.DateFrom.HasValue) query = query.Where(a => a.Timestamp >= filter.DateFrom.Value.Date);
            if (filter.DateTo.HasValue) query = query.Where(a => a.Timestamp < filter.DateTo.Value.Date.AddDays(1));

            filter.TotalCount = await query.CountAsync();
            filter.TotalPages = Math.Max(1, (int)Math.Ceiling(filter.TotalCount / (double)filter.PageSize));
            if (filter.Page > filter.TotalPages) filter.Page = filter.TotalPages;

            var rows = await query
                .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Format in memory (ToString with a format can't be translated to SQL)
            filter.Logs = rows.Select(a =>
            {
                var vm = new AuditLogListVM
                {
                    Id = a.Id,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    Module = a.Module,
                    RecordId = a.RecordId,
                    RecordName = a.RecordName,
                    Timestamp = a.Timestamp.ToString("dd MMM yyyy, hh:mm:ss tt"),
                    IpAddress = a.IpAddress,
                    Source = a.Source,
                    RequestPath = a.RequestPath
                };
                ParseDetails(vm, a.Details);
                return vm;
            }).ToList();

            return View(filter);
        }

        // Stored JSON: {"changes":[{field,old,new}]}  |  {"values":{field:value}}  |  {"note":"..."}
        // Anything else (older free-text rows) is shown as plain text.
        private static void ParseDetails(AuditLogListVM vm, string? details)
        {
            if (string.IsNullOrWhiteSpace(details)) return;

            try
            {
                using var doc = JsonDocument.Parse(details);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("changes", out var ch) && ch.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in ch.EnumerateArray())
                            vm.Changes.Add(new AuditChangeVM
                            {
                                Field = Str(c, "field") ?? "",
                                Old = Str(c, "old"),
                                New = Str(c, "new")
                            });
                        return;
                    }
                    if (root.TryGetProperty("values", out var vals) && vals.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in vals.EnumerateObject())
                            vm.Values.Add(new AuditChangeVM
                            {
                                Field = p.Name,
                                New = p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ToString()
                            });
                        return;
                    }
                    if (root.TryGetProperty("note", out var note))
                    {
                        vm.DetailsText = note.ToString();
                        return;
                    }
                }
            }
            catch (JsonException) { /* not JSON -> fall through */ }

            vm.DetailsText = details;
        }

        private static string? Str(JsonElement e, string name)
            => e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;
    }
}