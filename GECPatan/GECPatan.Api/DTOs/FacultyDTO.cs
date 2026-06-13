namespace GECPatan.Api.DTOs
{
    // ── LIST (for /api/faculty) ─────────────────────────────
    public class FacultyListDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public bool IsTeaching { get; set; }
        public int SeniorityOrder { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string? DeptShortCode { get; set; }
    }

    // ── DETAIL (for /api/faculty/{id}) ──────────────────────
    public class FacultyDetailDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? Website { get; set; }
        public bool IsTeaching { get; set; }
        public string DateOfJoining { get; set; } = string.Empty;

        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string? DeptShortCode { get; set; }

        // Public-safe personal info (no contact/email exposed)
        public List<FacultyQualificationDTO> Qualifications { get; set; } = new();
        public List<FacultyExperienceDTO> Experiences { get; set; } = new();
        public List<FacultyTrainingDTO> Trainings { get; set; } = new();
        public List<FacultyPublicationDTO> Publications { get; set; } = new();
    }

    public class FacultyQualificationDTO
    {
        public string Degree { get; set; } = string.Empty;
        public string? University { get; set; }
        public string? Year { get; set; }
        public string? Specialization { get; set; }
    }

    public class FacultyExperienceDTO
    {
        public string Position { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }  // null = "Present"
    }

    public class FacultyTrainingDTO
    {
        public string Title { get; set; } = string.Empty;
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