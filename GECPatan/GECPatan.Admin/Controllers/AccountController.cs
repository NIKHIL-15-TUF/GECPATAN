using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
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

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context, 
            NotificationService notify)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _notify = notify;
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
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if account is active
            if (!user.IsActive)
            {
                ModelState.AddModelError("",
                    "Your account has been deactivated. Contact the administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Force password change check
                if (user.MustChangePassword)
                    return RedirectToAction(nameof(ChangePassword));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("",
                    "Account locked after too many attempts. Try again after 5 minutes.");
            else
                ModelState.AddModelError("", "Invalid email or password.");

            return View(model);
        }

        // ── FORCE CHANGE PASSWORD ─────────────────────────
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            // If they don't need to change, redirect to dashboard
            if (!user.MustChangePassword)
                return RedirectToAction("Index", "Home");

            ViewData["Title"] = "Change Password";
            ViewBag.IsForcedChange = true;
            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            ViewData["Title"] = "Change Password";
            ViewBag.IsForcedChange = true;

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Clear the force change flag
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            await _notify.SendAsync(
                title: $"{user.FullName} changed their password",
                message: "First-time password change completed",
                module: "Account",
                icon: "fa-key",
                color: "success",
                link: "/UserManagement/Index",
                forRole: "SuperAdmin"
            );
            TempData["Success"] = "Password changed successfully. Welcome!";
            return RedirectToAction("Index", "Home");
        }

        // ── LOGOUT ────────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // ── ACCESS DENIED ─────────────────────────────────
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewData["Layout"] = "_AccountLayout";
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}
