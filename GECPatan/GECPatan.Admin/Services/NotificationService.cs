using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GECPatan.Admin.Services
{
    /// <summary>
    /// Inject via constructor: private readonly NotificationService _notify;
    ///
    /// USAGE EXAMPLES:
    ///
    /// // 1. Notify all SuperAdmins
    /// await _notify.SendAsync(
    ///     title:   "New Faculty Added",
    ///     message: $"{faculty.Name} was added to {dept.Name}",
    ///     module:  "Faculty",
    ///     icon:    "fa-user-plus",
    ///     color:   "success",
    ///     link:    $"/Faculty/Edit/{faculty.FacultyId}",
    ///     forRole: "SuperAdmin"
    /// );
    ///
    /// // 2. Notify a specific user
    /// await _notify.SendAsync(
    ///     title:   "Your profile was updated",
    ///     message: "Admin updated your profile details.",
    ///     module:  "Faculty",
    ///     icon:    "fa-edit",
    ///     color:   "info",
    ///     link:    "/Faculty/Edit/1",
    ///     forUserId: "user-guid-here"
    /// );
    ///
    /// // 3. Notify all roles (broadcast)
    /// await _notify.SendAsync(
    ///     title:   "New News Published",
    ///     message: "Admission notice has been published.",
    ///     module:  "News",
    ///     icon:    "fa-newspaper",
    ///     color:   "primary"
    /// );
    ///
    /// ICON EXAMPLES:
    ///   "fa-user-plus"   → user added
    ///   "fa-edit"        → profile updated
    ///   "fa-newspaper"   → news added
    ///   "fa-file-alt"    → content page
    ///   "fa-key"         → password changed
    ///   "fa-file-contract" → tender added
    ///   "fa-trophy"      → achievement added
    ///
    /// COLOR OPTIONS: success / info / warning / danger / primary
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;

        public NotificationService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }

        /// <summary>
        /// Creates a notification.
        /// forRole=null    → all roles see it
        /// forUserId=null  → all users of that role see it
        /// </summary>
        public async Task SendAsync(
            string title,
            string? message = null,
            string? module = null,
            string icon = "fa-bell",
            string color = "info",
            string? link = null,
            string? forRole = null,
            string? forUserId = null)
        {
            var user = _httpContext.HttpContext?.User;

            _context.Notifications.Add(new Notification
            {
                Title = title,
                Message = message,
                Module = module,
                Icon = icon,
                IconColor = color,
                Link = link,
                ForRole = forRole,
                ForUserId = forUserId,
                TriggeredBy = user?.Identity?.Name ?? "System",
                TriggeredByRole = user?.FindFirst(
                    ClaimTypes.Role)?.Value ?? "",
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get unread count for current user.
        /// Filters by role + userId + 30-day window.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(
            string userId, string role)
        {
            var cutoff = DateTime.Now.AddDays(-30);

            // IDs already read by this user
            var readIds = await _context.NotificationReads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            return await _context.Notifications
                .Where(n => !n.IsDeleted
                    && n.CreatedDate >= cutoff
                    && !readIds.Contains(n.Id)
                    && (
                        // targeted to this user
                        n.ForUserId == userId
                        // targeted to this role
                        || (n.ForUserId == null &&
                            (n.ForRole == null || n.ForRole == role))
                    ))
                .CountAsync();
        }

        /// <summary>
        /// Get latest notifications for dropdown (max 10).
        /// Shows last 30 days only. Marks which are read.
        /// </summary>
        public async Task<List<NotificationItemVM>> GetLatestAsync(
            string userId, string role, int take = 10)
        {
            var cutoff = DateTime.Now.AddDays(-30);

            var readIds = await _context.NotificationReads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var items = await _context.Notifications
                .Where(n => !n.IsDeleted
                    && n.CreatedDate >= cutoff
                    && (
                        n.ForUserId == userId
                        || (n.ForUserId == null &&
                            (n.ForRole == null || n.ForRole == role))
                    ))
                .OrderByDescending(n => n.CreatedDate)
                .Take(take)
                .Select(n => new NotificationItemVM
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Module = n.Module,
                    Icon = n.Icon,
                    IconColor = n.IconColor,
                    Link = n.Link,
                    TriggeredBy = n.TriggeredBy,
                    CreatedDate = n.CreatedDate,
                    IsRead = readIds.Contains(n.Id)
                })
                .ToListAsync();

            return items;
        }

        /// <summary>
        /// Get ALL notifications for the list page
        /// with pagination. UI only shows 30-day window.
        /// </summary>
        public async Task<List<NotificationItemVM>> GetAllAsync(
            string userId, string role,
            string? module = null, bool? unreadOnly = null)
        {
            var cutoff = DateTime.Now.AddDays(-30);

            var readIds = await _context.NotificationReads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var query = _context.Notifications
                .Where(n => !n.IsDeleted
                    && n.CreatedDate >= cutoff
                    && (
                        n.ForUserId == userId
                        || (n.ForUserId == null &&
                            (n.ForRole == null || n.ForRole == role))
                    ))
                .AsQueryable();

            if (!string.IsNullOrEmpty(module))
                query = query.Where(n => n.Module == module);

            if (unreadOnly == true)
                query = query.Where(n => !readIds.Contains(n.Id));

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NotificationItemVM
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Module = n.Module,
                    Icon = n.Icon,
                    IconColor = n.IconColor,
                    Link = n.Link,
                    TriggeredBy = n.TriggeredBy,
                    CreatedDate = n.CreatedDate,
                    IsRead = readIds.Contains(n.Id)
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mark a single notification as read for this user.
        /// </summary>
        public async Task MarkReadAsync(int notificationId, string userId)
        {
            bool alreadyRead = await _context.NotificationReads
                .AnyAsync(r => r.NotificationId == notificationId
                            && r.UserId == userId);
            if (alreadyRead) return;

            _context.NotificationReads.Add(new NotificationRead
            {
                NotificationId = notificationId,
                UserId = userId,
                ReadAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Mark ALL visible notifications as read for this user.
        /// </summary>
        public async Task MarkAllReadAsync(string userId, string role)
        {
            var cutoff = DateTime.Now.AddDays(-30);

            var readIds = await _context.NotificationReads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var unread = await _context.Notifications
                .Where(n => !n.IsDeleted
                    && n.CreatedDate >= cutoff
                    && !readIds.Contains(n.Id)
                    && (
                        n.ForUserId == userId
                        || (n.ForUserId == null &&
                            (n.ForRole == null || n.ForRole == role))
                    ))
                .Select(n => n.Id)
                .ToListAsync();

            foreach (var id in unread)
            {
                _context.NotificationReads.Add(new NotificationRead
                {
                    NotificationId = id,
                    UserId = userId,
                    ReadAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }
    }

    // ── VM used by service ────────────────────────────────
    public class NotificationItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Module { get; set; }
        public string Icon { get; set; } = "fa-bell";
        public string IconColor { get; set; } = "info";
        public string? Link { get; set; }
        public string? TriggeredBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public string TimeAgo => GetTimeAgo(CreatedDate);

        private static string GetTimeAgo(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return dt.ToString("dd MMM");
        }
    }
}