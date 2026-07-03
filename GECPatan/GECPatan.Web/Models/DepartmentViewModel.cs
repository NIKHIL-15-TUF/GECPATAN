using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    // Mirrors the shape the old DepartmentVM / Department.cshtml expected,
    // but populated from API DTOs instead of a server-side ViewModel.
    public class DepartmentViewModel
    {
        public int DeptId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? TitleImagePath { get; set; }
        public string? Tagline { get; set; }

        public bool ShowIntake { get; set; }
        public int Intake { get; set; }
        public int AnnualPlacementCount { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }

        public List<string> BannerImages { get; set; } = new();
        public List<string> Vision { get; set; } = new();
        public List<string> Mission { get; set; } = new();
        public List<string> PEOs { get; set; } = new();
        public List<string> PSOs { get; set; } = new();

        public List<DeptLabDTO> Labs { get; set; } = new();
        public List<DeptFacultyDTO> FacultyList { get; set; } = new();
        public List<DeptTimetableDTO> TimeTable { get; set; } = new();
        public List<DeptIntakeDTO> IntakeHistory { get; set; } = new();
        public List<ActivityDTO> Activities { get; set; } = new();
        public List<DeptNoticeDTO> NoticeBoard { get; set; } = new();

        // Extra content the API exposes generically. Not present in the old view,
        // but wired up so any Mandatory-Disclosure-style section can render without
        // needing a new hardcoded tab every time.
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();

        // NOTE: Academic Calendar, Achievements and Newsletter have no corresponding
        // API endpoints yet on DepartmentsController. Their tabs are omitted below
        // until GetAcademicCalendar / GetAchievements / GetNewsletter are added,
        // following the same pattern as GetActivities / GetNotices.
    }
}
