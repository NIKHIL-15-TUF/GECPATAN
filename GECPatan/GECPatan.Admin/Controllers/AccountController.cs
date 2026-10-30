using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Filters;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using GECPatan.Core.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        private readonly ISecurityLogService _security;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            NotificationService notify,
            ISecurityLogService security)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _notify = notify;
            _security = security;
        }

        // ── LOGIN ─────────────────────────────────────────
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _security.LogAsync(HttpContext, SecurityEvents.LoginFailed, SecuritySeverity.Warning,
                    details: "Unknown email address", attemptedEmail: model.Email,
                    throttleKey: $"unknown|{HttpContext.Connection.RemoteIpAddress}|{model.Email}");
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if account is active
            if (!user.IsActive)
            {
                await _security.LogAsync(HttpContext, SecurityEvents.LoginBlockedInactive, SecuritySeverity.Warning,
                    details: "Login attempt on a deactivated account",
                    userId: user.Id, userName: user.UserName, attemptedEmail: model.Email);
                ModelState.AddModelError("",
                    "Your account has been deactivated. Contact the administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _security.LogAsync(HttpContext, SecurityEvents.LoginSuccess, SecuritySeverity.Info,
                    details: model.RememberMe ? "Remember-me login" : null,
                    userId: user.Id, userName: user.UserName, attemptedEmail: model.Email);

                // A fresh login always asks again (a previous "Change later" is forgotten)
                Response.Cookies.Delete(ForcePasswordChangeFilter.SkipCookie);

                if (user.MustChangePassword)
                    return RedirectToAction(nameof(ChangePassword));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                await _security.LogAsync(HttpContext, SecurityEvents.AccountLockedOut, SecuritySeverity.Critical,
                    details: "Account locked after too many failed password attempts",
                    userId: user.Id, userName: user.UserName, attemptedEmail: model.Email,
                    throttleKey: $"lockout|{user.Id}|{HttpContext.Connection.RemoteIpAddress}");
                ModelState.AddModelError("",
                    "Account locked after too many attempts. Try again after 5 minutes.");
            }
            else
            {
                var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                await _security.LogAsync(HttpContext, SecurityEvents.LoginFailed, SecuritySeverity.Warning,
                    details: $"Wrong password (failed attempt {failedCount})",
                    userId: user.Id, userName: user.UserName, attemptedEmail: model.Email);
                ModelState.AddModelError("", "Invalid email or password.");
            }

            return View(model);
        }

        // ── CHANGE PASSWORD ───────────────────────────────
        // Forced (temporary password) -> shows a "Change later" button.
        // Otherwise it is the normal voluntary change page (sidebar > Account).
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            ViewData["Title"] = "Change Password";
            ViewBag.IsForcedChange = user.MustChangePassword;
            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            ViewData["Title"] = "Change Password";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var wasForced = user.MustChangePassword;
            ViewBag.IsForcedChange = wasForced;

            if (!ModelState.IsValid) return View(model);

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                await _security.LogAsync(HttpContext, SecurityEvents.PasswordChangeFailed, SecuritySeverity.Warning,
                    details: string.Join("; ", result.Errors.Select(e => e.Code)),
                    userId: user.Id, userName: user.UserName);
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _security.LogAsync(HttpContext, SecurityEvents.PasswordChanged, SecuritySeverity.Info,
                details: wasForced ? "First-time / forced password change" : "Voluntary password change",
                userId: user.Id, userName: user.UserName);

            // Clear the force change flag
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            Response.Cookies.Delete(ForcePasswordChangeFilter.SkipCookie);

            await _notify.SendAsync(
                title: $"{user.FullName} changed their password",
                message: wasForced ? "First-time password change completed" : "Password changed",
                module: "Account",
                icon: "fa-key",
                color: "success",
                link: "/UserManagement/Index",
                forRole: "SuperAdmin"
            );
            TempData["Success"] = wasForced
                ? "Password changed successfully. Welcome!"
                : "Password changed successfully.";
            return RedirectToAction("Index", "Home");
        }

        // "Change later": let the user into the admin for this browser session.
        // The temporary password stays pending and is asked again at next login.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkipPasswordChange()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            if (user.MustChangePassword)
            {
                Response.Cookies.Append(ForcePasswordChangeFilter.SkipCookie, user.Id, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax          // no Expires = ends with the browser session
                });
                TempData["Success"] = "You can change your password any time from Account > Change Password in the side menu.";
            }
            return RedirectToAction("Index", "Home");
        }

        // ── LOGOUT ────────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Log first - after SignOutAsync the user's identity is gone
            await _security.LogAsync(HttpContext, SecurityEvents.Logout, SecuritySeverity.Info);
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete(ForcePasswordChangeFilter.SkipCookie);
            return RedirectToAction(nameof(Login));
        }

        // ── ACCESS DENIED ─────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> AccessDenied(string? returnUrl = null)
        {
            // Only signed-in users land here from a failed [Authorize(Roles=...)] check
            if (User.Identity?.IsAuthenticated == true)
                await _security.LogAsync(HttpContext, SecurityEvents.AccessDenied, SecuritySeverity.Warning,
                    details: $"Tried to open {returnUrl ?? "a restricted page"} without the required role",
                    statusCode: 403);

            ViewData["Layout"] = "_AccountLayout";
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}
