using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class CommitteeViewModel
    {
        // Base URL of GECPatan.Api, used to resolve uploaded file/image paths
        // (title image, member photos, flyer, etc.) -- same reasoning as
        // DepartmentViewModel.ApiBaseUrl.
        public string ApiBaseUrl { get; set; } = string.Empty;

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? Tagline { get; set; }
        public string? Measures { get; set; }
        public string? Message { get; set; }
        public string? TitleImagePath { get; set; }
        public string? TitleImageCSSClass { get; set; }
        public string? MeasureImagePath { get; set; }
        public string? SubObjImagePath { get; set; }
        public string? BulletPointsImagePath { get; set; }
        public string? PageFlyerPath { get; set; }
        public string? BlogLink { get; set; }
        public string? Link { get; set; }
        public string? Account { get; set; }
        public string? NationalTaskForce { get; set; }
        public bool ShowDocument { get; set; }
        public bool TableView { get; set; }

        // Tab labels are now CMS-driven per committee instead of hardcoded
        // (e.g. NSS uses "Register Grievance" for TabLink).
        public string TabAbout { get; set; } = "About";
        public string TabVisionMission { get; set; } = "Vision & Mission";
        public string TabObjectives { get; set; } = "Objectives";
        public string TabMembers { get; set; } = "Members";
        public string TabActivities { get; set; } = "Activities";
        public string TabDocuments { get; set; } = "Documents";
        public string TabLink { get; set; } = "Link";

        public List<string> Vision { get; set; } = new();
        public List<string> Mission { get; set; } = new();
        public List<string> Objectives { get; set; } = new();
        public List<string> SubObjectives { get; set; } = new();

        public List<CommitteeMemberDTO> Members { get; set; } = new();
        public List<CommitteeMemberGroupDTO> AdditionalMemberGroups { get; set; } = new();

        // NOTE: the committee-detail endpoint doesn't return activities data.
        // Populated from a separate GET /api/committees/{id}/activities call
        // (assumed, mirroring the Department pattern) -- confirm the route
        // once you have it; if it doesn't exist yet this just stays empty
        // and the Activities tab won't render.
        public List<ActivityDTO> Activities { get; set; } = new();

        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }
}