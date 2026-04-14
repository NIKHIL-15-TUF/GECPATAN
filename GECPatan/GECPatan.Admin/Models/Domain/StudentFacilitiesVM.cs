using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── STUDENT CLUB ──────────────────────────────────────
    public class StudentClubListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
        public int MemberCount { get; set; }
    }

    public class StudentClubCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? About { get; set; }
        public string? Icon { get; set; }
        public string? BlogLink { get; set; }
        public string? ActionName { get; set; }
        public string? ControllerName { get; set; }
        public bool IsDynamic { get; set; } = false;
        public int? DynamicId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    public class StudentClubEditVM : StudentClubCreateVM
    {
        public int Id { get; set; }
    }

    public class ClubMemberVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public int ClubId { get; set; }
        public string? ExistingImagePath { get; set; }
    }

    // ── FACILITY (generic for Hostel, Library, etc.) ──────
    public class FacilityVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Tagline { get; set; }
        public string? About { get; set; }

        // Vision/Mission stored as newline-separated
        public string? VisionItems { get; set; }
        public string? MissionItems { get; set; }

        public string? FacilityType { get; set; } // "Hostel", "Library", "Medical" etc.
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public string? ExistingTitleImagePath { get; set; }
    }
}