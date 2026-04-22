using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── USER LIST ─────────────────────────────────────────
    public class UserListVM
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Assignment { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── STEP 1: SELECT ROLE ───────────────────────────────
    public class UserStep1VM
    {
        [Required(ErrorMessage = "Please select a role")]
        public string Role { get; set; } = string.Empty;
    }

    // ── STEP 2: CREDENTIALS ───────────────────────────────
    public class UserStep2VM
    {
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── STEP 3: ASSIGNMENT ────────────────────────────────
    // Which dept / committee is this user managing?
    public class UserStep3VM
    {
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Assignment (depends on role)
        public int? DeptId { get; set; }
        public int? CommitteeId { get; set; }
        public int? FacultyId { get; set; }
        public int? FacilityId { get; set; }

        // Dropdowns
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Committees { get; set; } = new();
        public List<SelectListItem> Faculties { get; set; } = new();
        public List<SelectListItem> Facilities { get; set; } = new();
    }

    // ── EDIT USER ─────────────────────────────────────────
    public class UserEditVM
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // Email is VIEW ONLY — cannot be changed
        public string? Email { get; set; }
        public string? Role { get; set; }

        public int? DeptId { get; set; }
        public int? CommitteeId { get; set; }
        public int? FacultyId { get; set; }
        public int? FacilityId { get; set; }

        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }

        // Dropdowns
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Committees { get; set; } = new();
        public List<SelectListItem> Faculties { get; set; } = new();
        public List<SelectListItem> Facilities { get; set; } = new();
    }

    // ── RESET PASSWORD ────────────────────────────────────
    public class ResetPasswordVM
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── CHANGE PASSWORD (Force) ───────────────────────────
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}