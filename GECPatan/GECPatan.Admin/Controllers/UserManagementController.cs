using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "User Management";

            var users = await _context.Users
                .Cast<ApplicationUser>()
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var list = new List<UserListVM>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var role = roles.FirstOrDefault() ?? "—";

                string assignment = "";
                if (u.DeptId.HasValue)
                {
                    var dept = await _context.Departments.FindAsync(u.DeptId.Value);
                    assignment = dept?.Name ?? "";
                }
                else if (u.CommitteeId.HasValue)
                {
                    var comm = await _context.CampusCommittees.FindAsync(u.CommitteeId.Value);
                    assignment = comm?.Title ?? "";
                }
                else if (u.FacilityId.HasValue)
                {
                    var fac = await _context.Facilities.FindAsync(u.FacilityId.Value);
                    assignment = fac?.Title ?? "";
                }

                list.Add(new UserListVM
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = role,
                    Assignment = assignment,
                    IsActive = u.IsActive,
                    MustChangePassword = u.MustChangePassword,
                    CreatedDate = u.CreatedDate
                });
            }

            return View(list);
        }

        // ══════════════════════════════════════════════════
        // 5-STEP USER CREATION FLOW
        // ══════════════════════════════════════════════════

        // ── STEP 1: SELECT ROLE ───────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Create User — Step 1: Select Role";
            return View("CreateStep1", new UserStep1VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateStep1(UserStep1VM model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create User — Step 1: Select Role";
                return View(model);
            }
            return RedirectToAction(nameof(CreateStep2),
                new { role = model.Role });
        }

        // ── STEP 2: CREDENTIALS ───────────────────────────
        public IActionResult CreateStep2(string role)
        {
            ViewData["Title"] = $"Create User — Step 2: Credentials ({role})";
            return View(new UserStep2VM { Role = role });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep2Post(UserStep2VM model)
        {
            ViewData["Title"] = $"Create User — Step 2: Credentials ({model.Role})";

            // Check email uniqueness
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
                ModelState.AddModelError("Email",
                    "This email is already registered.");

            // SuperAdmin: only 1 allowed
            if (model.Role == AppRoles.SuperAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
                if (admins.Count >= 1)
                    ModelState.AddModelError("",
                        "Only one SuperAdmin is allowed.");
            }

            // Principal: only 1 allowed
            if (model.Role == AppRoles.Principal)
            {
                var principals = await _userManager.GetUsersInRoleAsync(AppRoles.Principal);
                if (principals.Count >= 1)
                    ModelState.AddModelError("",
                        "Only one Principal account is allowed.");
            }

            if (!ModelState.IsValid) return View("CreateStep2", model);

            // Roles that need assignment → go to step 3
            var rolesNeedingAssignment = new[]
            {
                AppRoles.HOD,
                AppRoles.CommitteeHead,
                AppRoles.Faculty
            };

            if (rolesNeedingAssignment.Contains(model.Role))
            {
                return RedirectToAction(nameof(CreateStep3), new
                {
                    role = model.Role,
                    fullName = model.FullName,
                    email = model.Email,
                    password = model.Password
                });
            }

            // Roles that don't need assignment → create directly
            return await CreateUser(model.Role, model.FullName,
                model.Email, model.Password,
                null, null, null, null);
        }

        // ── STEP 3: ASSIGNMENT ────────────────────────────
        public async Task<IActionResult> CreateStep3(string role,
            string fullName, string email, string password)
        {
            ViewData["Title"] = $"Create User — Step 3: Assignment ({role})";

            var vm = new UserStep3VM
            {
                Role = role,
                FullName = fullName,
                Email = email,
                Password = password
            };

            await PopulateAssignmentDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep3Post(UserStep3VM model)
        {
            ViewData["Title"] = $"Create User — Step 3: Assignment ({model.Role})";

            // Validate assignment based on role
            if (model.Role == AppRoles.HOD && !model.DeptId.HasValue)
                ModelState.AddModelError("DeptId",
                    "Please select a department for HOD.");

            if (model.Role == AppRoles.CommitteeHead && !model.CommitteeId.HasValue)
                ModelState.AddModelError("CommitteeId",
                    "Please select a committee for Committee Head.");

            if (model.Role == AppRoles.Faculty && !model.FacultyId.HasValue)
                ModelState.AddModelError("FacultyId",
                    "Please select a faculty profile.");

            if (!ModelState.IsValid)
            {
                await PopulateAssignmentDropdowns(model);
                return View(model);
            }

            return await CreateUser(model.Role, model.FullName,
                model.Email, model.Password,
                model.DeptId, model.CommitteeId,
                model.FacultyId, model.FacilityId);
        }

        // ── CREATE USER HELPER ────────────────────────────
        private async Task<IActionResult> CreateUser(
            string role, string fullName, string email, string password,
            int? deptId, int? committeeId, int? facultyId, int? facilityId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                DeptId = deptId,
                CommitteeId = committeeId,
                FacultyId = facultyId,
                FacilityId = facilityId,
                IsActive = true,
                MustChangePassword = true,   // Force password change on first login
                CreatedDate = DateTime.Now,
                CreatedBy = currentUser?.Email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ",
                    result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Create));
            }

            // Assign role
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] =
                $"User '{fullName}' created with role '{role}'. " +
                "They will be prompted to change their password on first login.";

            return RedirectToAction(nameof(Index));
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["Title"] = "Edit User";
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            var appUser = u as ApplicationUser;
            if (appUser == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(appUser);

            var vm = new UserEditVM
            {
                Id = appUser.Id,
                FullName = appUser.FullName ?? "",
                Email = appUser.Email,
                Role = roles.FirstOrDefault(),
                DeptId = appUser.DeptId,
                CommitteeId = appUser.CommitteeId,
                FacultyId = appUser.FacultyId,
                FacilityId = appUser.FacilityId,
                IsActive = appUser.IsActive,
                MustChangePassword = appUser.MustChangePassword
            };

            await PopulateEditDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditVM model)
        {
            ViewData["Title"] = "Edit User";
            if (!ModelState.IsValid)
            {
                await PopulateEditDropdowns(model);
                return View(model);
            }

            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            // Email + Dept CANNOT be changed (view only)
            appUser.FullName = model.FullName;
            appUser.DeptId = model.DeptId;
            appUser.CommitteeId = model.CommitteeId;
            appUser.FacultyId = model.FacultyId;
            appUser.FacilityId = model.FacilityId;
            appUser.IsActive = model.IsActive;
            appUser.MustChangePassword = model.MustChangePassword;

            await _userManager.UpdateAsync(appUser);

            TempData["Success"] = $"User '{appUser.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var appUser = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (appUser == null) return NotFound();

            // Cannot deactivate yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            appUser.IsActive = !appUser.IsActive;
            await _userManager.UpdateAsync(appUser);

            TempData["Success"] = $"'{appUser.FullName}' " +
                (appUser.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── RESET PASSWORD ────────────────────────────────
        public async Task<IActionResult> ResetPassword(string id)
        {
            ViewData["Title"] = "Reset Password";
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            var appUser = u as ApplicationUser;

            return View(new ResetPasswordVM
            {
                UserId = id,
                UserName = appUser?.FullName ?? u.Email
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

            // Remove existing password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(u);
            var result = await _userManager.ResetPasswordAsync(u, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Force user to change password on next login
            var appUser = u as ApplicationUser;
            if (appUser != null)
            {
                appUser.MustChangePassword = true;
                await _userManager.UpdateAsync(appUser);
            }

            TempData["Success"] =
                $"Password reset for '{model.UserName}'. " +
                "They will be prompted to change it on next login.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task PopulateAssignmentDropdowns(UserStep3VM vm)
        {
            // Departments — exclude already assigned to an HOD
            var assignedDeptIds = await _context.Users
                .OfType<ApplicationUser>()
                .Where(u => u.DeptId != null)
                .Select(u => u.DeptId!.Value)
                .ToListAsync();

            vm.Departments = await _context.Departments
                .Where(d => !assignedDeptIds.Contains((int)d.DeptId))
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            // Committees — exclude already assigned
            var assignedCommitteeIds = await _context.Users
                .OfType<ApplicationUser>()
                .Where(u => u.CommitteeId != null)
                .Select(u => u.CommitteeId!.Value)
                .ToListAsync();

            vm.Committees = await _context.CampusCommittees
                .Where(c => !assignedCommitteeIds.Contains(c.Id))
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Title
                }).ToListAsync();

            // Faculties — exclude already linked to a user
            var assignedFacultyIds = await _context.Users
                .OfType<ApplicationUser>()
                .Where(u => u.FacultyId != null)
                .Select(u => u.FacultyId!.Value)
                .ToListAsync();

            vm.Faculties = await _context.Faculties
                .Include(f => f.Department)
                .Where(f => !assignedFacultyIds.Contains(f.FacultyId))
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.Name + " (" +
                        (f.Department != null ? f.Department.Name : "") + ")"
                }).ToListAsync();

            // Facilities — exclude already assigned
            var assignedFacilityIds = await _context.Users
                .OfType<ApplicationUser>()
                .Where(u => u.FacilityId != null)
                .Select(u => u.FacilityId!.Value)
                .ToListAsync();

            vm.Facilities = await _context.Facilities
                .Where(f => !assignedFacilityIds.Contains(f.Id))
                .OrderBy(f => f.Title)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Title
                }).ToListAsync();
        }
        private async Task PopulateEditDropdowns(UserEditVM vm)
        {
            vm.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.Committees = await _context.CampusCommittees
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Title
                }).ToListAsync();

            vm.Faculties = await _context.Faculties
                .Include(f => f.Department)
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.Name
                }).ToListAsync();

            vm.Facilities = await _context.Facilities
                .OrderBy(f => f.Title)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Title
                }).ToListAsync();
        }
    }
}