namespace GECPatan.Web.Models.Dtos
{
    // Generic wrapper matching ApiResponse<T>.Ok(...) / .Fail(...) from the API project.
    // If your actual ApiResponse<T> uses different property names/casing, update here.
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
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

    public class DynamicSectionDTO
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? SectionType { get; set; }
        public string? HtmlContent { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public int DisplayOrder { get; set; }
        public List<DynamicSectionFileDTO> Files { get; set; } = new();
    }

    public class DynamicSectionFileDTO
    {
        public string? FilePath { get; set; }
        public string? Title { get; set; }
        public string? FileType { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class DeptFacultyDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? ImagePath { get; set; }
        public string? AreaOfInterest { get; set; }
        public bool IsTeaching { get; set; }
        public int SeniorityOrder { get; set; }
    }

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

    public class DeptTimetableDTO
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int? SemesterType { get; set; }
        public int Semester { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? UploadedDate { get; set; }
    }

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
    public class DeptIntakeDTO
    {
        public int IntakeYear { get; set; }
        public int Intake { get; set; }
        public bool IsLatest { get; set; }
    }

    public class ActivityDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EventDate { get; set; }
        public string? EventTime { get; set; }
        public int Year { get; set; }
        public string? TargetStudents { get; set; }
        public string? Keywords { get; set; }
        public string? ExternalLink { get; set; }
        public string? Link { get; set; }
        public List<ActivityImageDTO> Images { get; set; } = new();

        public List<ActivityFileDTO> Files { get; set; } = new();
    }

    public class ActivityImageDTO
    {
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ActivityFileDTO
    {
        public string? Title { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public int DisplayOrder { get; set; }
    }
}
