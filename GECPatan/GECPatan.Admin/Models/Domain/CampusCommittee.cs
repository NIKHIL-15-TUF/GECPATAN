using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    public class CampusCommittee : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? About { get; set; }
        public string? Tagline { get; set; }
        public string? Measures { get; set; }
        public string? Message { get; set; }

        // Images
        public string? TitleImagePath { get; set; }
        public string? TitleImageCSSClass { get; set; }
        public string? MeasureImagePath { get; set; }
        public string? SubObjImagePath { get; set; }
        public string? BulletPointsImagePath { get; set; }
        public string? PageFlyerPath { get; set; }

        // Links
        public string? BlogLink { get; set; }
        public string? Link { get; set; }
        public string? Account { get; set; }
        public string? NationalTaskForce { get; set; }

        // Display flags
        public bool ShowDocument { get; set; } = false;
        public bool TableView { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        // Tab title overrides
        public string TabAbout { get; set; } = "About";
        public string TabVisionMission { get; set; } = "Vision & Mission";
        public string TabObjectives { get; set; } = "Objectives";
        public string TabMembers { get; set; } = "Members";
        public string TabActivities { get; set; } = "Activities";
        public string TabDocuments { get; set; } = "Documents";
        public string TabLink { get; set; } = "Register Grievance";

        // Navigation
        [ValidateNever]
        public ICollection<CommitteeVision> Visions { get; set; }
            = new List<CommitteeVision>();

        [ValidateNever]
        public ICollection<CommitteeMission> Missions { get; set; }
            = new List<CommitteeMission>();

        [ValidateNever]
        public ICollection<CommitteeObjective> Objectives { get; set; }
            = new List<CommitteeObjective>();

        [ValidateNever]
        public ICollection<CommitteeSubObjective> SubObjectives { get; set; }
            = new List<CommitteeSubObjective>();

        [ValidateNever]
        public ICollection<CommitteeMember> Members { get; set; }
            = new List<CommitteeMember>();

        [ValidateNever]
        public ICollection<AdditionalMemberGroup> AdditionalMemberGroups { get; set; }
            = new List<AdditionalMemberGroup>();
    }
}