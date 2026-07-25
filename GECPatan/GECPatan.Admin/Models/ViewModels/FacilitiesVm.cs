using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class FacilityListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? TitleImagePath { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public string? BlogspotLink { get; set; }
        public int MemberCount { get; set; }
        public int SectionCount { get; set; }
    }

    public class FacilityCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Tagline { get; set; }

        public string? About { get; set; }
        public string? BlogspotLink { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class FacilityEditVM : FacilityCreateVM
    {
        public int Id { get; set; }
        public string? ExistingTitleImagePath { get; set; }
        public List<string> VisionItems { get; set; } = new();
        public List<string> MissionItems { get; set; } = new();

        // Read-only stats
        public int MemberCount { get; set; }
        public int SectionCount { get; set; }
    }

    public class FacilityMemberVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public int FacilityId { get; set; }
        public string? ExistingImagePath { get; set; }
    }

    //// ── STUDENT CLUB ──────────────────────────────────────
    public class StudentClubListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ImageCount { get; set; }
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
        public string? BlogLink { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public IFormFile? CoverImageFile { get; set; }
    }

    public class StudentClubEditVM : StudentClubCreateVM
    {
        public int Id { get; set; }
        public string? CoverImagePath { get; set; }

        public List<ClubImageVM> ExistingImages { get; set; } = new();
    }

    public class ClubImageVM
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
        public int ClubId { get; set; }
    }

    public class ClubMemberVM : IValidatableObject
    {
        public int Id { get; set; }
        public int ClubId { get; set; }

        [Required(ErrorMessage = "Choose Student or Faculty")]
        public string MemberType { get; set; } = "Student"; // "Student" | "Faculty"

        // Faculty path
        public int? FacultyId { get; set; }

        // Student path
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        // Common — role within the club, required either way
        [Required(ErrorMessage = "Position is required")]
        [MaxLength(200)]
        public string? Position { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public List<SelectListItem> Faculties { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            if (MemberType == "Faculty" && (FacultyId is null or 0))
                yield return new ValidationResult("Please select a faculty member.", new[] { nameof(FacultyId) });

            if (MemberType == "Student" && string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required for a student member.", new[] { nameof(Name) });
        }
    }
}
