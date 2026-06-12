using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
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

        // ── DASHBOARD ROUTER ──────────────────────────────
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null
                ? await _userManager.GetRolesAsync(user)
                : new List<string>();
            var role = roles.FirstOrDefault() ?? "";

            return role switch
            {
                AppRoles.Principal => await PrincipalDashboard(user),
                AppRoles.HOD => await HodDashboard(user),
                AppRoles.Faculty => await FacultyDashboard(user),
                AppRoles.ContentEditor => await ContentEditorDashboard(user),
                AppRoles.PlacementOfficer => await PlacementDashboard(),
                AppRoles.CommitteeHead => await CommitteeHeadDashboard(user),
                AppRoles.GrievanceCoordinator => await GrievanceDashboard(),
                _ => await AdminDashboard(user)
            };
        }

        // ══════════════════════════════════════════════════
        // SUPER ADMIN DASHBOARD (existing)
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> AdminDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "Dashboard";
            var now = DateTime.Now;

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

            var pendingPwd = await _context.Users
                .OfType<ApplicationUser>()
                .CountAsync(u => u.MustChangePassword && u.IsActive);
            if (pendingPwd > 0)
                vm.Alerts.Add(
                    $"{pendingPwd} user(s) haven't changed their password yet.");

            return View("Dashboard/Admin", vm);
        }

        // ══════════════════════════════════════════════════
        // PRINCIPAL DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> PrincipalDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "Principal Dashboard";

            var vm = new PrincipalDashboardVM
            {
                DepartmentCount = await _context.Departments.CountAsync(),
                FacultyCount = await _context.Faculties.CountAsync(),
                CommitteeCount = await _context.CampusCommittees.CountAsync(),
                NewsCount = await _context.NewsItems.CountAsync(),
                ActiveUsersCount = await _context.Users
                    .OfType<ApplicationUser>()
                    .CountAsync(u => u.IsActive),

                RecentNews = await _context.NewsItems
                    .OrderByDescending(n => n.PublishDate)
                    .Take(5)
                    .Select(n => new RecentItemVM
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Date = n.PublishDate.HasValue
                            ? n.PublishDate.Value.ToString("dd MMM yyyy")
                            : ""
                    }).ToListAsync(),

                RecentActivities = await _context.Activities
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(5)
                    .Select(a => new RecentItemVM
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Date = a.CreatedDate.ToString("dd MMM yyyy")
                    }).ToListAsync(),

                PendingPasswordUsers = await _context.Users
                    .OfType<ApplicationUser>()
                    .CountAsync(u => u.MustChangePassword && u.IsActive),

                ActivePrincipal = await _context.Principals
                    .Where(p => p.IsActive)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync()
            };

            return View("Dashboard/Principal", vm);
        }

        // ══════════════════════════════════════════════════
        // HOD DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> HodDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "HOD Dashboard";

            if (user?.DeptId == null)
                return View("Dashboard/NoDeptAssigned");

            int deptId = user.DeptId.Value;

            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DeptId == deptId);

            if (dept == null) return View("Dashboard/NoDeptAssigned");

            var intake = await _context.ProgramIntakes
                .Where(p => p.DeptId == deptId)
                .OrderByDescending(p => p.IntakeYear)
                .Select(p => new { p.Intake, p.IntakeYear })
                .FirstOrDefaultAsync();

            var labCount = await _context.Labs
                .CountAsync(l => l.DeptId == deptId);
            var facultyCount = await _context.Faculties
                .CountAsync(f => f.DeptId == deptId && f.IsActive);

            var recentFaculty = await _context.Faculties
                .Where(f => f.DeptId == deptId)
                .OrderByDescending(f => f.CreatedDate)
                .Take(5)
                .Select(f => new RecentItemVM
                {
                    Id = f.FacultyId,
                    Title = f.Name,
                    Date = f.Designation
                })
                .ToListAsync();

            var notices = await _context.DeptNotices
                .Where(n => n.DeptId == deptId
                    && n.IsVisible
                    && (!n.ValidTo.HasValue || n.ValidTo >= DateTime.Now))
                .CountAsync();

            var vm = new HodDashboardVM
            {
                DeptId = deptId,
                DeptName = dept.Name,
                ShortCode = dept.ShortCode,
                FacultyCount = facultyCount,
                LabCount = labCount,
                CurrentIntake = intake?.Intake ?? 0,
                IntakeYear = intake?.IntakeYear ?? 0,
                ActiveNotices = notices,
                RecentFaculty = recentFaculty
            };

            return View("Dashboard/HOD", vm);
        }

        // ══════════════════════════════════════════════════
        // FACULTY DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> FacultyDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "My Profile";

            if (user?.FacultyId == null)
                return View("Dashboard/NoProfileAssigned");

            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .Include(f => f.Qualifications)
                .Include(f => f.Experiences)
                .Include(f => f.Trainings)
                .Include(f => f.Publications)
                .FirstOrDefaultAsync(f => f.FacultyId == user.FacultyId);

            if (faculty == null)
                return View("Dashboard/NoProfileAssigned");

            var notices = await _context.DeptNotices
                .Where(n => n.DeptId == faculty.DeptId
                    && n.IsVisible
                    && (!n.ValidTo.HasValue || n.ValidTo >= DateTime.Now))
                .OrderBy(n => n.DisplayOrder)
                .Take(5)
                .Select(n => new RecentItemVM
                {
                    Id = n.Id,
                    Title = n.Title,
                    Date = n.ValidTo.HasValue
                        ? $"Until {n.ValidTo.Value:dd MMM yyyy}"
                        : "No expiry"
                })
                .ToListAsync();

            var vm = new FacultyDashboardVM
            {
                FacultyId = faculty.FacultyId,
                Name = faculty.Name,
                Designation = faculty.Designation,
                DeptName = faculty.Department?.Name ?? "",
                PhotoPath = faculty.ImagePath,
                IsTeaching = faculty.IsTeaching,
                QualificationCount = faculty.Qualifications.Count,
                ExperienceCount = faculty.Experiences.Count,
                TrainingCount = faculty.Trainings.Count,
                PublicationCount = faculty.Publications.Count,
                DeptNotices = notices
            };

            return View("Dashboard/Faculty", vm);
        }

        // ══════════════════════════════════════════════════
        // CONTENT EDITOR DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> ContentEditorDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "My Content Page";

            // No page assigned
            if (user?.ContentPageId == null)
            {
                return View("Dashboard/ContentEditor",
                    new ContentEditorDashboardVM());
            }

            var page = await _context.ContentPages
                .FindAsync(user.ContentPageId.Value);

            if (page == null)
            {
                return View("Dashboard/ContentEditor",
                    new ContentEditorDashboardVM());
            }

            return View("Dashboard/ContentEditor",
                new ContentEditorDashboardVM
                {
                    PageId = page.Id,
                    PageTitle = page.Title,
                    PageSlug = page.Slug,
                    IsPublished = page.IsVisible,
                    ContentPreview = page.HtmlContent,
                    LastUpdated = page.UpdatedDate
                });
        }

        // ══════════════════════════════════════════════════
        // PLACEMENT OFFICER DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> PlacementDashboard()
        {
            ViewData["Title"] = "Placement Dashboard";

            var latestStat = await _context.PlacementStatistics
                .OrderByDescending(p => p.Year)
                .FirstOrDefaultAsync();

            var vm = new PlacementDashboardVM
            {
                StatCount = await _context.PlacementStatistics.CountAsync(),
                TeamCount = await _context.PlacementTeamMembers.CountAsync(),
                RecruiterCount = await _context.TopRecruiters.CountAsync(),
                LatestYear = latestStat?.Year ?? "",
                LatestTotalPlaced = latestStat?.TotalPlaced ?? 0,
                LatestTotalStudents = latestStat?.TotalStudents ?? 0,
                LatestHighestPkg = latestStat?.HighestPackage ?? "",
                LatestAveragePkg = latestStat?.AveragePackage ?? "",

                AllStats = await _context.PlacementStatistics
                    .OrderByDescending(p => p.Year)
                    .Take(5)
                    .Select(p => new RecentItemVM
                    {
                        Id = p.Id,
                        Title = $"{p.TotalPlaced} placed of {p.TotalStudents}",
                        Date = p.Year ?? ""
                    }).ToListAsync()
            };

            return View("Dashboard/PlacementOfficer", vm);
        }

        // ══════════════════════════════════════════════════
        // COMMITTEE HEAD DASHBOARD
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> CommitteeHeadDashboard(
            ApplicationUser? user)
        {
            ViewData["Title"] = "Committee Dashboard";

            if (user?.CommitteeId == null)
                return View("Dashboard/NoCommitteeAssigned");

            int commId = user.CommitteeId.Value;

            var committee = await _context.CampusCommittees
                .FirstOrDefaultAsync(c => c.Id == commId);

            if (committee == null)
                return View("Dashboard/NoCommitteeAssigned");

            var memberCount = await _context.CommitteeMembers
                .CountAsync(m => m.CommitteeId == commId);

            var activityCount = await _context.Activities
                .CountAsync(a => a.CommitteeId == commId);

            var recentActivities = await _context.Activities
                .Where(a => a.CommitteeId == commId)
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .Select(a => new RecentItemVM
                {
                    Id = a.Id,
                    Title = a.Title,
                    Date = a.CreatedDate.ToString("dd MMM yyyy")
                }).ToListAsync();

            var vm = new CommitteeHeadDashboardVM
            {
                CommitteeId = commId,
                CommitteeTitle = committee.Title,
                CommitteeTagline = committee.Tagline,
                MemberCount = memberCount,
                ActivityCount = activityCount,
                RecentActivities = recentActivities
            };

            return View("Dashboard/CommitteeHead", vm);
        }

        // ══════════════════════════════════════════════════
        // GRIEVANCE COORDINATOR DASHBOARD
        // ══════════════════════════════════════════════════
        private Task<IActionResult> GrievanceDashboard()
        {
            ViewData["Title"] = "Grievance Dashboard";
            return Task.FromResult<IActionResult>(
                View("Dashboard/GrievanceCoordinator"));
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
            if (PrincipalPhoto != null && PrincipalPhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    _env.WebRootPath, "uploads", "principal");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = "principal"
                    + Path.GetExtension(PrincipalPhoto.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await PrincipalPhoto.CopyToAsync(stream);
                await SaveSetting("Principal.Photo",
                    $"/uploads/principal/{fileName}");
            }
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
            var s = await _context.SiteSettings
                .FirstOrDefaultAsync(x => x.Key == key);
            return s?.Value;
        }

        private async Task SaveSetting(string key, string value)
        {
            var s = await _context.SiteSettings
                .FirstOrDefaultAsync(x => x.Key == key);
            if (s == null)
                _context.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = value,
                    Group = key.Split('.')[0]
                });
            else s.Value = value;
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
