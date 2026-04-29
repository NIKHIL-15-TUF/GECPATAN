using GECPatan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GECPatan.Admin.Models.Domain;
using System.Security.Claims;

namespace GECPatan.Admin.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notify;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            NotificationService notify,
            UserManager<ApplicationUser> userManager)
        {
            _notify = notify;
            _userManager = userManager;
        }

        // ── FULL LIST PAGE ────────────────────────────────
        public async Task<IActionResult> Index(
            string? module = null, bool? unreadOnly = null)
        {
            ViewData["Title"] = "Notifications";

            var (userId, role) = await GetUserInfo();

            var items = await _notify.GetAllAsync(
                userId, role, module, unreadOnly);

            // Modules for filter dropdown
            ViewBag.Modules = items
                .Where(n => !string.IsNullOrEmpty(n.Module))
                .Select(n => n.Module!)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            ViewBag.Module = module;
            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.TotalUnread = await _notify
                .GetUnreadCountAsync(userId, role);

            return View(items);
        }

        // ── MARK ONE READ + REDIRECT TO LINK ─────────────
        [HttpGet]
        public async Task<IActionResult> Read(int id, string? link)
        {
            var (userId, _) = await GetUserInfo();
            await _notify.MarkReadAsync(id, userId);

            if (!string.IsNullOrEmpty(link))
                return Redirect(link);

            return RedirectToAction(nameof(Index));
        }

        // ── MARK ALL READ ─────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var (userId, role) = await GetUserInfo();
            await _notify.MarkAllReadAsync(userId, role);
            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET BELL DATA (AJAX — called by layout) ───────
        [HttpGet]
        public async Task<IActionResult> GetBellData()
        {
            var (userId, role) = await GetUserInfo();

            var count = await _notify.GetUnreadCountAsync(userId, role);
            var items = await _notify.GetLatestAsync(userId, role, 10);

            return Json(new { count, items });
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