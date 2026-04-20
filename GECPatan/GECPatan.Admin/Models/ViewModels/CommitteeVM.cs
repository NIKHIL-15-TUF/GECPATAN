using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GECPatan.Admin.Models.ViewModels
{
    public class CommitteeListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? TitleImagePath { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int MemberCount { get; set; }
    }

    public class CommitteeCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // About is now RTE — stored as HTML
        public string? About { get; set; }
        public string? Tagline { get; set; }
        public string? Measures { get; set; }
        public string? Message { get; set; }
        public string? BlogLink { get; set; }
        public string? Link { get; set; }

        // Account field REMOVED as per meeting notes

        public string? NationalTaskForce { get; set; }
        public bool ShowDocument { get; set; } = false;
        public bool TableView { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        // Tab title overrides
        public string TabAbout { get; set; } = "About";
        public string TabVisionMission { get; set; } = "Vision & Mission";
        public string TabObjectives { get; set; } = "Objectives";
        public string TabMembers { get; set; } = "Members";
        public string TabActivities { get; set; } = "Activities";
        public string TabDocuments { get; set; } = "Documents";
        public string TabLink { get; set; } = "Register Grievance";
    }

    public class CommitteeEditVM : CommitteeCreateVM
    {
        public int Id { get; set; }
        public string? ExistingTitleImagePath { get; set; }
        public string? ExistingMeasureImagePath { get; set; }
        public string? ExistingSubObjImagePath { get; set; }
        public string? ExistingBulletPointsImagePath { get; set; }
        public string? ExistingPageFlyerPath { get; set; }

        public List<string> VisionItems { get; set; } = new();
        public List<string> MissionItems { get; set; } = new();
        public List<string> ObjectiveItems { get; set; } = new();
        public List<string> SubObjectiveItems { get; set; } = new();
    }

    // Member VM — supports Faculty dropdown + manual entry
    public class CommitteeMemberVM
    {
        public int Id { get; set; }

        // If FacultyId selected → auto-fill details
        public int? FacultyId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public int CommitteeId { get; set; }

        // For dropdown
        public List<SelectListItem> FacultyList { get; set; } = new();
    }
}