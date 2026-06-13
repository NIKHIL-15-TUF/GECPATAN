namespace GECPatan.Api.DTOs
{
    // ── LIST (for /api/departments) ────────────────────────
    public class DepartmentListDTO
    {
        public int DeptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? Tagline { get; set; }
        public string? TitleImagePath { get; set; }
        public int DisplayOrder { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int CurrentIntake { get; set; }
    }

    // ── DETAIL (for /api/departments/{id}) ─────────────────
    public class DepartmentDetailDTO
    {
        public int DeptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? About { get; set; }
        public string? TitleImagePath { get; set; }
        public string? Tagline { get; set; }
        public bool ShowIntake { get; set; }
        public int AnnualPlacement { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int CurrentIntake { get; set; }
        public int CurrentIntakeYear { get; set; }

        public List<string> BannerImages { get; set; } = new();
        public List<string> Visions { get; set; } = new();
        public List<string> Missions { get; set; } = new();
        public List<string> PEOs { get; set; } = new();
        public List<string> PSOs { get; set; } = new();
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }

    // ── FACULTY (for /api/departments/{id}/faculty) ────────
    public class DeptFacultyDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public bool IsTeaching { get; set; }
        public int SeniorityOrder { get; set; }
    }

    // ── LABS (for /api/departments/{id}/labs) ──────────────
    public class DeptLabDTO
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public string? About { get; set; }
        public int DisplayOrder { get; set; }
        public List<DeptLabImageDTO> Images { get; set; } = new();
    }

    public class DeptLabImageDTO
    {
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── TIMETABLE (for /api/departments/{id}/timetable) ────
    public class DeptTimetableDTO
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int SemesterType { get; set; } // 1=Odd, 2=Even
        public int Semester { get; set; }
        public string? FilePath { get; set; }
        public string UploadedDate { get; set; } = string.Empty;
    }

    // ── INTAKE (for /api/departments/{id}/intake) ──────────
    public class DeptIntakeDTO
    {
        public int IntakeYear { get; set; }
        public int Intake { get; set; }
        public bool IsLatest { get; set; }
    }

    // ── NOTICES (for /api/departments/{id}/notices) ────────
    public class DeptNoticeDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public string? FileType { get; set; }
        public string? ExternalLink { get; set; }
        public string? ValidFrom { get; set; }
        public string? ValidTo { get; set; }
        public string? PostedBy { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── DYNAMIC SECTIONS (generic, attached to dept) ───────
    public class DynamicSectionDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SectionType { get; set; } = string.Empty; // RichText/PDFViewer/etc
        public string? HtmlContent { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public int DisplayOrder { get; set; }
        public List<DynamicSectionFileDTO> Files { get; set; } = new();
    }

    public class DynamicSectionFileDTO
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FileType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}