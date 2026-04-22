using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public HomeController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ── DASHBOARD ─────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            var user = await _userManager.GetUserAsync(User);
            var appUser = user as ApplicationUser;

            var vm = new DashboardVM
            {
                DepartmentCount = await _context.Departments.CountAsync(),
                FacultyCount = await _context.Faculties.CountAsync(),
                CommitteeCount = await _context.CampusCommittees.CountAsync(),
                NewsCount = await _context.NewsItems.CountAsync(),
                ActivityCount = await _context.Activities.CountAsync(),
                AchievementCount = await _context.Achievements.CountAsync(),
                StudentClubCount = await _context.StudentClubs.CountAsync(),
                AlumniCount = await _context.Alumni.CountAsync(),
                ContentPageCount = await _context.ContentPages.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                FacilityCount = await _context.Facilities.CountAsync()
            };

            // Recent audit logs (last 8 actions)
            vm.RecentLogs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .Select(a => new RecentActivityVM
                {
                    UserName = a.UserName,
                    Action = a.Action,
                    Module = a.Module,
                    Record = a.RecordName,
                    TimeAgo = GetTimeAgo(a.Timestamp)
                })
                .ToListAsync();

            // Alerts
            var alerts = new List<string>();

            var pendingPwd = await _context.Users
                .OfType<ApplicationUser>()
                .CountAsync(u => u.MustChangePassword && u.IsActive);
            if (pendingPwd > 0)
                alerts.Add($"{pendingPwd} user(s) haven't changed their password yet.");

            vm.Alerts = alerts;

            return View(vm);
        }

        // ── HOME PAGE SETTINGS ────────────────────────────
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> HomePageSettings()
        {
            ViewData["Title"] = "Home Page Settings";

            var vm = new HomePageSettingsVM
            {
                Vision = await GetSetting("HomePage.Vision"),
                Mission = await GetSetting("HomePage.Mission"),
                PrincipalName = await GetSetting("Principal.Name"),
                PrincipalDesignation = await GetSetting("Principal.Designation"),
                PrincipalMessage = await GetSetting("Principal.Message"),
                ExistingPrincipalPhoto = await GetSetting("Principal.Photo"),
                EstablishedYear = await GetSetting("College.EstablishedYear"),
                CollegeTagline = await GetSetting("College.Tagline"),
                FacebookUrl = await GetSetting("Social.Facebook"),
                TwitterUrl = await GetSetting("Social.Twitter"),
                YouTubeUrl = await GetSetting("Social.YouTube"),
                LinkedInUrl = await GetSetting("Social.LinkedIn"),
                InstagramUrl = await GetSetting("Social.Instagram"),
                Phone = await GetSetting("Contact.Phone"),
                Email = await GetSetting("Contact.Email"),
                Address = await GetSetting("Contact.Address")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> HomePageSettings(
            HomePageSettingsVM model, IFormFile? PrincipalPhoto)
        {
            ViewData["Title"] = "Home Page Settings";

            // Handle principal photo upload
            if (PrincipalPhoto != null && PrincipalPhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "principal");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = "principal" + Path.GetExtension(PrincipalPhoto.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await PrincipalPhoto.CopyToAsync(stream);
                await SaveSetting("Principal.Photo", $"/uploads/principal/{fileName}");
            }

            // Save all settings
            await SaveSetting("HomePage.Vision", model.Vision ?? "");
            await SaveSetting("HomePage.Mission", model.Mission ?? "");
            await SaveSetting("Principal.Name", model.PrincipalName ?? "");
            await SaveSetting("Principal.Designation", model.PrincipalDesignation ?? "");
            await SaveSetting("Principal.Message", model.PrincipalMessage ?? "");
            await SaveSetting("College.EstablishedYear", model.EstablishedYear ?? "");
            await SaveSetting("College.Tagline", model.CollegeTagline ?? "");
            await SaveSetting("Social.Facebook", model.FacebookUrl ?? "");
            await SaveSetting("Social.Twitter", model.TwitterUrl ?? "");
            await SaveSetting("Social.YouTube", model.YouTubeUrl ?? "");
            await SaveSetting("Social.LinkedIn", model.LinkedInUrl ?? "");
            await SaveSetting("Social.Instagram", model.InstagramUrl ?? "");
            await SaveSetting("Contact.Phone", model.Phone ?? "");
            await SaveSetting("Contact.Email", model.Email ?? "");
            await SaveSetting("Contact.Address", model.Address ?? "");

            TempData["Success"] = "Home page settings saved.";
            return RedirectToAction(nameof(HomePageSettings));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<string?> GetSetting(string key)
        {
            var setting = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        private async Task SaveSetting(string key, string value)
        {
            var setting = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                _context.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = value,
                    Group = key.Split('.')[0]
                });
            }
            else
            {
                setting.Value = value;
            }

            await _context.SaveChangesAsync();
        }

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