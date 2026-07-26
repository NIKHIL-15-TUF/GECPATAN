using GECPatan.Core.Data;
using GECPatan.Core.Models.Common;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using GECPatan.Core.Services.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserManagementController : Controller
    {
        private const int PageSize = 25;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        private readonly UserDirectoryService _directory;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            NotificationService notify,
            UserDirectoryService directory,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _notify = notify;
            _directory = directory;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════
        // INDEX
        // ══════════════════════════════════════════════════
        // Previously: one GetRolesAsync() call plus one FindAsync() call per
        // user, inside a foreach loop — up to 2N extra DB round-trips for N
        // users. For "thousands of users" (the actual target scale for this
        // app) this page would time out. Now: 2 queries total via
        // UserDirectoryService, regardless of user count, plus pagination so
        // the page itself never has to render an unbounded table.
        public async Task<IActionResult> Index(int page = 1)
        {
            ViewData["Title"] = "User Management";

            if (page < 1) page = 1;

            var query = _context.Users
                .OfType<ApplicationUser>()
                .OrderBy(u => u.FullName);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var roles = await _directory.GetPrimaryRolesAsync(users.Select(u => u.Id));
            var assignments = await _directory.GetAssignmentNamesAsync(users);

            var list = users.Select(u => new UserListVM
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = roles.GetValueOrDefault(u.Id, "—"),
                Assignment = assignments.GetValueOrDefault(u.Id, ""),
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                CreatedDate = u.CreatedDate
            }).ToList();

            return View(new PagedResult<UserListVM>
            {
                Items = list,
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            });
        }

        // ══════════════════════════════════════════════════
        // STEP 1 — SELECT ROLE
        // ══════════════════════════════════════════════════
        public IActionResult Create()
        {
            ViewData["Title"] = "Create User — Step 1";
            TempData.Remove("UserRole");
            TempData.Remove("UserFullName");
            TempData.Remove("UserEmail");
            TempData.Remove("UserPassword");
            return View("CreateStep1", new UserStep1VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateStep1(UserStep1VM model)
        {
            ViewData["Title"] = "Create User — Step 1";
            if (!ModelState.IsValid) return View(model);

            TempData["UserRole"] = model.Role;
            return RedirectToAction(nameof(CreateStep2));
        }

        // ══════════════════════════════════════════════════
        // STEP 2 — CREDENTIALS
        // ══════════════════════════════════════════════════
        public IActionResult CreateStep2()
        {
            var role = TempData.Peek("UserRole")?.ToString();
            if (string.IsNullOrEmpty(role))
                return RedirectToAction(nameof(Create));

            ViewData["Title"] = $"Create User — Step 2 ({role})";
            return View(new UserStep2VM { Role = role });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep2Post(UserStep2VM model)
        {
            ViewData["Title"] = $"Create User — Step 2 ({model.Role})";
            TempData["UserRole"] = model.Role;

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
                ModelState.AddModelError("Email", "This email is already registered.");

            if (model.Role == AppRoles.SuperAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
                if (admins.Count >= 1)
                    ModelState.AddModelError("", "Only one SuperAdmin is allowed.");
            }

            if (model.Role == AppRoles.Principal)
            {
                var principals = await _userManager.GetUsersInRoleAsync(AppRoles.Principal);
                if (principals.Count >= 1)
                    ModelState.AddModelError("", "Only one Principal account is allowed.");
            }

            if (!ModelState.IsValid) return View("CreateStep2", model);

            // Store in TempData — safe for special chars
            TempData["UserRole"] = model.Role;
            TempData["UserFullName"] = model.FullName;
            TempData["UserEmail"] = model.Email;
            TempData["UserPassword"] = model.Password;

            // These roles need Step 3 assignment
            var rolesNeedingAssignment = new[]
            {
                AppRoles.HOD,
                AppRoles.CommitteeHead,
                AppRoles.Faculty,
                AppRoles.ContentEditor
            };

            if (rolesNeedingAssignment.Contains(model.Role))
                return RedirectToAction(nameof(CreateStep3));

            // No assignment needed — create directly
            return await CreateUserFromTempData();
        }

        // ══════════════════════════════════════════════════
        // STEP 3 — ASSIGNMENT
        // ══════════════════════════════════════════════════
        public async Task<IActionResult> CreateStep3()
        {
            var role = TempData.Peek("UserRole")?.ToString();
            var fullName = TempData.Peek("UserFullName")?.ToString();
            var email = TempData.Peek("UserEmail")?.ToString();
            var password = TempData.Peek("UserPassword")?.ToString();

            if (string.IsNullOrEmpty(role) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Session expired. Please start again.";
                return RedirectToAction(nameof(Create));
            }

            ViewData["Title"] = $"Create User — Step 3 ({role})";

            var vm = new UserStep3VM
            {
                Role = role,
                FullName = fullName ?? "",
                Email = email,
                Password = password
            };

            await PopulateAssignmentDropdowns(vm, role);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep3Post(UserStep3VM model)
        {
            ViewData["Title"] = $"Create User — Step 3 ({model.Role})";

            // Keep TempData alive for validation failures
            TempData["UserRole"] = model.Role;
            TempData["UserFullName"] = model.FullName;
            TempData["UserEmail"] = model.Email;
            TempData["UserPassword"] = model.Password;

            // Role-specific validation
            if (model.Role == AppRoles.HOD && !model.DeptId.HasValue)
                ModelState.AddModelError("DeptId",
                    "Please select a department for HOD.");

            if (model.Role == AppRoles.CommitteeHead && !model.CommitteeId.HasValue)
                ModelState.AddModelError("CommitteeId",
                    "Please select a committee for Committee Head.");

            if (model.Role == AppRoles.Faculty && !model.FacultyId.HasValue)
                ModelState.AddModelError("FacultyId",
                    "Please select a faculty profile.");

            if (model.Role == AppRoles.ContentEditor && !model.ContentPageId.HasValue)
                ModelState.AddModelError("ContentPageId",
                    "Please select a content page for Content Editor role.");

            // ContentEditor — ContentPageId is optional, no validation needed

            if (!ModelState.IsValid)
            {
                await PopulateAssignmentDropdowns(model, model.Role);
                return View(model);
            }

            return await CreateUser(
                role: model.Role,
                fullName: model.FullName,
                email: model.Email,
                password: model.Password,
                deptId: model.DeptId,
                committeeId: model.CommitteeId,
                facultyId: model.FacultyId,
                facilityId: model.FacilityId,
                contentPageId: model.ContentPageId  // ← properly passed
            );
        }

        // ══════════════════════════════════════════════════
        // CREATE USER — from TempData (no assignment roles)
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> CreateUserFromTempData()
        {
            var role = TempData["UserRole"]?.ToString() ?? "";
            var fullName = TempData["UserFullName"]?.ToString() ?? "";
            var email = TempData["UserEmail"]?.ToString() ?? "";
            var password = TempData["UserPassword"]?.ToString() ?? "";

            // No assignment for these roles
            return await CreateUser(
                role: role,
                fullName: fullName,
                email: email,
                password: password,
                deptId: null,
                committeeId: null,
                facultyId: null,
                facilityId: null,
                contentPageId: null
            );
        }

        // ══════════════════════════════════════════════════
        // CREATE USER — core method (all params explicit)
        // ══════════════════════════════════════════════════
        private async Task<IActionResult> CreateUser(
            string role,
            string fullName,
            string email,
            string password,
            int? deptId,
            int? committeeId,
            int? facultyId,
            int? facilityId,
            int? contentPageId)   // ← in signature, no more 'model' reference
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                DeptId = deptId,
                CommitteeId = committeeId,
                FacultyId = facultyId,
                FacilityId = facilityId,
                ContentPageId = contentPageId,
                IsActive = true,
                MustChangePassword = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUser?.Email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("User creation failed for {Email}: {Errors}",
                    email, string.Join(" | ", result.Errors.Select(e => e.Description)));
                TempData["Error"] = string.Join(" | ",
                    result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Create));
            }

            // Ensure role exists then assign
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            var roleResult = await _userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                // The Identity user was created but couldn't be assigned a role —
                // leaving it in place would create a "ghost" account with no
                // permissions that nobody can log in and use meaningfully.
                // Delete it so Create is all-or-nothing from the admin's point of view.
                _logger.LogError(
                    "Role assignment failed for new user {Email} (role {Role}): {Errors}. Rolling back user creation.",
                    email, role, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));

                await _userManager.DeleteAsync(user);

                TempData["Error"] = "The user account could not be fully created (role assignment failed). Please try again.";
                return RedirectToAction(nameof(Create));
            }

            // Clear TempData
            TempData.Remove("UserRole");
            TempData.Remove("UserFullName");
            TempData.Remove("UserEmail");
            TempData.Remove("UserPassword");

            _logger.LogInformation("User {Email} created with role {Role} by {AdminEmail}",
                email, role, currentUser?.Email);

            TempData["Success"] =
                $" User '{fullName}' created with role '{role}'. " +
                "They will be prompted to change their password on first login.";

            // Best-effort: the user is already created and usable at this point,
            // so a notification failure is logged, not surfaced as a request error.
            try
            {
                await _notify.SendAsync(
                    title: $"New User Created: {user.FullName}",
                    message: $"Role: {role}",
                    module: "UserManagement",
                    icon: "fa-user-plus",
                    color: "success",
                    link: "/UserManagement/Index",
                    forRole: "SuperAdmin"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification for new user {Email}", email);
            }

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // EDIT USER
        // ══════════════════════════════════════════════════
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["Title"] = "Edit User";

            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault();

            var vm = new UserEditVM
            {
                Id = appUser.Id,
                FullName = appUser.FullName ?? "",
                Email = appUser.Email,
                Role = role,
                DeptId = appUser.DeptId,
                CommitteeId = appUser.CommitteeId,
                FacultyId = appUser.FacultyId,
                FacilityId = appUser.FacilityId,
                ContentPageId = appUser.ContentPageId,
                IsActive = appUser.IsActive,
                MustChangePassword = appUser.MustChangePassword
            };

            await PopulateEditDropdowns(vm, role);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditVM model)
        {
            ViewData["Title"] = "Edit User";

            if (!ModelState.IsValid)
            {
                await PopulateEditDropdowns(model, model.Role);
                return View(model);
            }

            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            appUser.FullName = model.FullName;
            appUser.DeptId = model.DeptId;
            appUser.CommitteeId = model.CommitteeId;
            appUser.FacultyId = model.FacultyId;
            appUser.FacilityId = model.FacilityId;
            appUser.ContentPageId = model.ContentPageId;
            appUser.IsActive = model.IsActive;
            appUser.MustChangePassword = model.MustChangePassword;

            var result = await _userManager.UpdateAsync(appUser);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ",
                    result.Errors.Select(e => e.Description));
                return View(model);
            }

            TempData["Success"] = $"User '{appUser.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // TOGGLE ACTIVE
        // ══════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            appUser.IsActive = !appUser.IsActive;
            await _userManager.UpdateAsync(appUser);

            _logger.LogInformation("User {Email} {Action} by {AdminEmail}",
                appUser.Email, appUser.IsActive ? "activated" : "deactivated", currentUser?.Email);

            TempData["Success"] = $"'{appUser.FullName}' " +
                (appUser.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // RESET PASSWORD
        // ══════════════════════════════════════════════════
        public async Task<IActionResult> ResetPassword(string id)
        {
            ViewData["Title"] = "Reset Password";

            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            return View(new ResetPasswordVM
            {
                UserId = id,
                UserName = appUser.FullName ?? appUser.Email ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            ViewData["Title"] = "Reset Password";
            if (!ModelState.IsValid) return View(model);

            var u = await _userManager.FindByIdAsync(model.UserId);
            if (u == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(u);
            var result = await _userManager.ResetPasswordAsync(u, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(x => x.Id == model.UserId);

            if (appUser != null)
            {
                appUser.MustChangePassword = true;
                await _userManager.UpdateAsync(appUser);
            }

            var adminUser = await _userManager.GetUserAsync(User);
            _logger.LogWarning("Password reset for user {Email} by admin {AdminEmail}",
                appUser?.Email, adminUser?.Email);

            TempData["Success"] =
                $"Password reset for '{model.UserName}'. " +
                "They will be prompted to change it on next login.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private async Task PopulateAssignmentDropdowns(UserStep3VM vm, string role)
        {
            // HOD → Department (unassigned only)
            if (role == AppRoles.HOD)
            {
                var assignedDeptIds = await _context.Users
                    .OfType<ApplicationUser>()
                    .Where(u => u.DeptId != null)
                    .Select(u => u.DeptId!.Value)
                    .ToListAsync();

                vm.Departments = await _context.Departments
                    .Where(d => d.IsActive && !assignedDeptIds.Contains((int)d.DeptId))
                    .OrderBy(d => d.Name)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DeptId.ToString(),
                        Text = d.Name
                    }).ToListAsync();
            }

            // CommitteeHead → Committee (unassigned only)
            if (role == AppRoles.CommitteeHead)
            {
                var assignedCommIds = await _context.Users
                    .OfType<ApplicationUser>()
                    .Where(u => u.CommitteeId != null)
                    .Select(u => u.CommitteeId!.Value)
                    .ToListAsync();

                vm.Committees = await _context.CampusCommittees
                    .Where(c => !assignedCommIds.Contains(c.Id))
                    .OrderBy(c => c.Title)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Title
                    }).ToListAsync();
            }

            // Faculty → Faculty profile (unlinked only)
            if (role == AppRoles.Faculty)
            {
                var assignedFacultyIds = await _context.Users
                    .OfType<ApplicationUser>()
                    .Where(u => u.FacultyId != null)
                    .Select(u => u.FacultyId!.Value)
                    .ToListAsync();

                vm.Faculties = await _context.Faculties
                    .Include(f => f.Department)
                    .Where(f => f.IsActive && !assignedFacultyIds.Contains(f.FacultyId))
                    .OrderBy(f => f.Name)
                    .Select(f => new SelectListItem
                    {
                        Value = f.FacultyId.ToString(),
                        Text = f.Name +
                            (f.Department != null ? $" ({f.Department.Name})" : "")
                    }).ToListAsync();
            }

            // ContentEditor → Content Page (optional, all pages shown)
            if (role == AppRoles.ContentEditor)
            {
                var assignedContentPageIds = await _context.Users
                    .OfType<ApplicationUser>()
                    .Where(u => u.ContentPageId.HasValue)
                    .Select(u => u.ContentPageId!.Value)
                    .ToListAsync();

                vm.ContentPages = await _context.ContentPages
                    .Where(d => !assignedContentPageIds.Contains(d.Id))
                    .OrderBy(d => d.Title)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Title
                    })
                    .ToListAsync();
            }

            //if (role == AppRoles.ContentEditor)
            //{
            //    vm.ContentPages = await _context.ContentPages
            //        .OrderBy(p => p.Title)
            //        .Select(p => new SelectListItem
            //        {
            //            Value = p.Id.ToString(),
            //            Text = p.Title
            //        }).ToListAsync();
            //}
        }

        private async Task PopulateEditDropdowns(UserEditVM vm, string? role)
        {
            if (role == AppRoles.HOD)
            {
                vm.Departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DeptId.ToString(),
                        Text = d.Name
                    }).ToListAsync();
            }

            if (role == AppRoles.CommitteeHead)
            {
                vm.Committees = await _context.CampusCommittees
                    .OrderBy(c => c.Title)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Title
                    }).ToListAsync();
            }

            if (role == AppRoles.Faculty)
            {
                vm.Faculties = await _context.Faculties
                    .Include(f => f.Department)
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Name)
                    .Select(f => new SelectListItem
                    {
                        Value = f.FacultyId.ToString(),
                        Text = f.Name +
                            (f.Department != null ? $" ({f.Department.Name})" : "")
                    }).ToListAsync();
            }

            if (role == AppRoles.ContentEditor)
            {
                var assignedContentPageIds = await _context.Users
                  .OfType<ApplicationUser>()
                  .Where(u => u.ContentPageId.HasValue)
                  .Select(u => u.ContentPageId!.Value)
                  .ToListAsync();

                vm.ContentPages = await _context.ContentPages
                     .Where(p => !assignedContentPageIds.Contains(p.Id)
                                  || p.Id == vm.ContentPageId)
                     .OrderBy(p => p.Title)
                     .Select(p => new SelectListItem
                     {
                         Value = p.Id.ToString(),
                         Text = p.Title
                     })
                     .ToListAsync();
            }
        }
    }
}