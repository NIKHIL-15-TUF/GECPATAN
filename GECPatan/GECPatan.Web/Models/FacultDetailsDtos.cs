using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class FacultyDetailsViewModel
    {
        // Base URL of GECPatan.Api, used to resolve ImagePath (see
        // ResolveApiFileUrl in the view) -- same reasoning as
        // DepartmentViewModel.ApiBaseUrl.
        public string ApiBaseUrl { get; set; } = string.Empty;

        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? Website { get; set; }
        public bool IsTeaching { get; set; }
        public string? DateOfJoining { get; set; }
        public string? DeptName { get; set; }
        public string? DeptShortCode { get; set; }

        public List<FacultyQualificationDTO> EducationalQualifications { get; set; } = new();
        public List<FacultyExperienceDTO> ProfessionalExperiences { get; set; } = new();
        public List<FacultyTrainingDTO> TrainingAndWorkshops { get; set; } = new();
        public List<FacultyPublicationDTO> Publications { get; set; } = new();

        // NOTE: the old FacultyDetailsVM also had an "Others" (Achievements /
        // Memberships) table. The current faculty API response doesn't
        // include that data yet, so it's omitted here until the endpoint
        // returns it.
    }
}