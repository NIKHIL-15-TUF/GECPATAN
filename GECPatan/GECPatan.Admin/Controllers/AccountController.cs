using GECPatan.Admin.Data;
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
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
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
//using GECPatan.Admin.Models.Domain;
//using GECPatan.Admin.Models.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;

//namespace GECPatan.Admin.Controllers
//{
//    public class AccountController : Controller
//    {
//        private readonly SignInManager<ApplicationUser> _signInManager;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public AccountController(
//            SignInManager<ApplicationUser> signInManager,
//            UserManager<ApplicationUser> userManager)
//        {
//            _signInManager = signInManager;
//            _userManager = userManager;
//        }

//        // ─ LOGIN GET ─
//        [HttpGet]
//        public IActionResult Login(string? returnUrl = null)
//        {
//            // Already logged in → go to dashboard
//            if (User.Identity?.IsAuthenticated == true)
//                return RedirectToAction("Index", "Home");

//            ViewData["ReturnUrl"] = returnUrl;
//            ViewData["Layout"] = "_AccountLayout";
//            return View();
//        }

//        // ── LOGIN POST ────────────────────────────────────
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
//        {
//            ViewData["ReturnUrl"] = returnUrl;
//            ViewData["Layout"] = "_AccountLayout";

//            if (!ModelState.IsValid)
//                return View(model);

//            var user = await _userManager.FindByEmailAsync(model.Email);

//            if (user == null)
//            {
//                ModelState.AddModelError("", "Invalid email or password.");
//                return View(model);
//            }

//            if (!user.IsActive)
//            {
//                ModelState.AddModelError("", "Your account has been deactivated. Please contact admin.");
//                return View(model);
//            }

//            var result = await _signInManager.PasswordSignInAsync(
//                model.Email,
//                model.Password,
//                model.RememberMe,
//                lockoutOnFailure: true);

//            if (result.Succeeded)
//            {
//                // Update last login date
//                user.LastLoginDate = DateTime.Now;
//                await _userManager.UpdateAsync(user);

//                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
//                    return Redirect(returnUrl);

//                return RedirectToAction("Index", "Home");
//            }

//            if (result.IsLockedOut)
//            {
//                ModelState.AddModelError("", "Account locked out due to multiple failed attempts. Try again in 15 minutes.");
//                return View(model);
//            }

//            ModelState.AddModelError("", "Invalid email or password.");
//            return View(model);
//        }

//        // ── LOGOUT ───────────────────────────────────────
//        [Authorize]
//        public async Task<IActionResult> Logout()
//        {
//            await _signInManager.SignOutAsync();
//            return RedirectToAction("Login", "Account");
//        }

//        // ── ACCESS DENIED ─────────────────────────────────
//        public IActionResult AccessDenied()
//        {
//            ViewData["Layout"] = "_AccountLayout";
//            return View();
//        }
//    }
//}