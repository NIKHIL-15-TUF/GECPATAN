using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── SLIDER ───
    public class SliderVM
    {
        public int Id { get; set; }

        [Display(Name = "Heading (H3)")]
        public string? H3Text { get; set; }

        [Display(Name = "Sub Heading (H4)")]
        public string? H4Text { get; set; }

        [Display(Name = "Small Text (H5)")]
        public string? H5Text { get; set; }

        [Display(Name = "Button 1 Text")]
        public string? Anchor1Text { get; set; }

        [Display(Name = "Button 1 Link")]
        public string? Anchor1Link { get; set; }

        [Display(Name = "Button 2 Text")]
        public string? Anchor2Text { get; set; }

        [Display(Name = "Button 2 Link")]
        public string? Anchor2Link { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ExistingImagePath { get; set; }
    }

    // ── MARQUEE ──
    public class MarqueeVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Type")]
        public int MarqueeType { get; set; } = 1;

        [Display(Name = "File Link (PDF URL)")]
        public string? FileLink { get; set; }

        [Display(Name = "Controller Name")]
        public string? ControllerName { get; set; }

        [Display(Name = "Action Name")]
        public string? ActionName { get; set; }

        [Display(Name = "Dynamic ID")]
        public int? DynamicId { get; set; }

        public bool IsFile { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── TESTIMONIAL ────
    public class TestimonialVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student name is required")]
        [MaxLength(200)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(20)]
        [Display(Name = "Passout Year")]
        public string? PassoutYear { get; set; }

        [Required(ErrorMessage = "Testimonial text is required")]
        [Display(Name = "Testimonial")]
        public string TestimonialText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── TOP RECRUITER ──
    public class TopRecruiterVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ExistingLogoPath { get; set; }
    }
    public class DashboardVM
    {
        // Counts
        public int DepartmentCount { get; set; }
        public int FacultyCount { get; set; }
        public int CommitteeCount { get; set; }
        public int NewsCount { get; set; }
        public int ActivityCount { get; set; }
        public int AchievementCount { get; set; }
        public int StudentClubCount { get; set; }
        public int AlumniCount { get; set; }
        public int ContentPageCount { get; set; }
        public int UserCount { get; set; }
        public int FacilityCount { get; set; }

        // Recent activity
        public List<RecentActivityVM> RecentLogs { get; set; } = new();

        // Quick alerts
        public List<string> Alerts { get; set; } = new();
    }

    public class RecentActivityVM
    {
        public string? UserName { get; set; }
        public string? Action { get; set; }
        public string? Module { get; set; }
        public string? Record { get; set; }
        public string? TimeAgo { get; set; }
    }

    // ── HOME PAGE SETTINGS ────────────────────────────────
    public class HomePageSettingsVM
    {
        // Vision & Mission
        [Display(Name = "Institute Vision")]
        public string? Vision { get; set; }

        [Display(Name = "Institute Mission")]
        public string? Mission { get; set; }

        // Principal Message
        [Display(Name = "Principal Name")]
        [MaxLength(200)]
        public string? PrincipalName { get; set; }

        [Display(Name = "Principal Designation")]
        [MaxLength(200)]
        public string? PrincipalDesignation { get; set; }

        [Display(Name = "Principal Message")]
        public string? PrincipalMessage { get; set; }

        [Display(Name = "Principal Photo")]
        public string? ExistingPrincipalPhoto { get; set; }

        // College Info
        [Display(Name = "College Established Year")]
        public string? EstablishedYear { get; set; }

        [Display(Name = "College Tagline")]
        [MaxLength(300)]
        public string? CollegeTagline { get; set; }

        // Social Media
        [Display(Name = "Facebook URL")]
        public string? FacebookUrl { get; set; }

        [Display(Name = "Twitter URL")]
        public string? TwitterUrl { get; set; }

        [Display(Name = "YouTube URL")]
        public string? YouTubeUrl { get; set; }

        [Display(Name = "LinkedIn URL")]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "Instagram URL")]
        public string? InstagramUrl { get; set; }

        // Contact
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }
    }
}
