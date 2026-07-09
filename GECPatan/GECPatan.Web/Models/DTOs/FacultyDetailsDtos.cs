namespace GECPatan.Web.Models.Dtos
{
    public class FacultyDetailDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? Website { get; set; }
        public bool IsTeaching { get; set; }
        public string? DateOfJoining { get; set; }
        public int DeptId { get; set; }
        public string? DeptName { get; set; }
        public string? DeptShortCode { get; set; }
        public List<FacultyQualificationDTO> Qualifications { get; set; } = new();
        public List<FacultyExperienceDTO> Experiences { get; set; } = new();
        public List<FacultyTrainingDTO> Trainings { get; set; } = new();
        public List<FacultyPublicationDTO> Publications { get; set; } = new();
    }

    public class FacultyQualificationDTO
    {
        public string? Degree { get; set; }
        public string? University { get; set; }
        public string? Year { get; set; }
        public string? Specialization { get; set; }
    }

    public class FacultyExperienceDTO
    {
        public string? Position { get; set; }
        public string? Organization { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }

    public class FacultyTrainingDTO
    {
        public string? Title { get; set; }
        public string? OrganizedBy { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }

    public class FacultyPublicationDTO
    {
        public int SrNo { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}