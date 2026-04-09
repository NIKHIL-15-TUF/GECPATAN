using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // ─ LOGIN GET ─
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Already logged in → go to dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Layout"] = "_AccountLayout";
            return View();
        }

        // ── LOGIN POST ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Layout"] = "_AccountLayout";

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact admin.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Update last login date
                user.LastLoginDate = DateTime.Now;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked out due to multiple failed attempts. Try again in 15 minutes.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ── LOGOUT ───────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ── ACCESS DENIED ─────────────────────────────────
        public IActionResult AccessDenied()
        {
            ViewData["Layout"] = "_AccountLayout";
            return View();
        }
    }
}