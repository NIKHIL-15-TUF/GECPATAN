using GECPatan.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GECPatan.Core.Models.Domain;

namespace GECPatan.Admin.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notify;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            NotificationService notify,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationController> logger)
        {
            _notify = notify;
            _userManager = userManager;
            _logger = logger;
        }

        // ── FULL LIST PAGE ────────────────────────────────
        public async Task<IActionResult> Index(string? module = null, bool? unreadOnly = null)
        {
            ViewData["Title"] = "Notifications";

            var (userId, role) = await GetUserInfo();

            var items = await _notify.GetAllAsync(userId, role, module, unreadOnly);

            ViewBag.Modules = items
                .Where(n => !string.IsNullOrEmpty(n.Module))
                .Select(n => n.Module!)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            ViewBag.Module = module;
            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.TotalUnread = await _notify.GetUnreadCountAsync(userId, role);

            return View(items);
        }

        // ── MARK ONE READ + REDIRECT TO LINK ─────────────
        [HttpGet]
        public async Task<IActionResult> Read(int id, string? link)
        {
            var (userId, _) = await GetUserInfo();

            try
            {
                await _notify.MarkReadAsync(id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} read for user {UserId}", id, userId);
                // Non-fatal: still honor the redirect so the user isn't blocked
                // from reaching the linked content just because the read-receipt failed.
            }

            // Only ever redirect to a local path — the original redirected to
            // `link` unconditionally, which is an open-redirect vulnerability:
            // a crafted notification (or a crafted URL shared to a user, e.g.
            // ".../Notification/Read?id=1&link=https://evil.example.com") would
            // send an authenticated user's browser to an arbitrary external site.
            if (!string.IsNullOrEmpty(link) && Url.IsLocalUrl(link))
                return Redirect(link);

            return RedirectToAction(nameof(Index));
        }

        // ── MARK ALL READ ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var (userId, role) = await GetUserInfo();

            try
            {
                await _notify.MarkAllReadAsync(userId, role);
                _logger.LogInformation("All notifications marked read for user {UserId}", userId);
                TempData["Success"] = "All notifications marked as read.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications read for user {UserId}", userId);
                TempData["Error"] = "Unable to mark notifications as read. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── GET BELL DATA (AJAX — called by layout) ───────
        [HttpGet]
        public async Task<IActionResult> GetBellData()
        {
            var (userId, role) = await GetUserInfo();

            try
            {
                var count = await _notify.GetUnreadCountAsync(userId, role);
                var items = await _notify.GetLatestAsync(userId, role, 10);

                return Json(new { count, items });
            }
            catch (Exception ex)
            {
                // This is polled by the shared layout on effectively every page load,
                // so a failure here must never bubble up as a 500 that could disrupt
                // unrelated pages. Degrade gracefully instead.
                _logger.LogError(ex, "Error fetching bell notification data for user {UserId}", userId);
                return Json(new { count = 0, items = Array.Empty<object>() });
            }
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<(string userId, string role)> GetUserInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "";
            var roles = user != null
                ? await _userManager.GetRolesAsync(user)
                : new List<string>();
            var role = roles.FirstOrDefault() ?? "";
            return (userId, role);
        }
    }
}