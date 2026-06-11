namespace GECPatan.Admin.Models.ViewModels
{
    public class RecentItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    // ── PRINCIPAL ─────────────────────────────────────────
    public class PrincipalDashboardVM
    {
        public int DepartmentCount { get; set; }
        public int FacultyCount { get; set; }
        public int CommitteeCount { get; set; }
        public int NewsCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int PendingPasswordUsers { get; set; }
        public string? ActivePrincipal { get; set; }
        public List<RecentItemVM> RecentNews { get; set; } = new();
        public List<RecentItemVM> RecentActivities { get; set; } = new();
    }

    // ── HOD ───────────────────────────────────────────────
    public class HodDashboardVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public int FacultyCount { get; set; }
        public int LabCount { get; set; }
        public int CurrentIntake { get; set; }
        public int IntakeYear { get; set; }
        public int ActiveNotices { get; set; }
        public List<RecentItemVM> RecentFaculty { get; set; } = new();
    }

    // ── FACULTY ───────────────────────────────────────────
    public class FacultyDashboardVM
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public bool IsTeaching { get; set; }
        public int QualificationCount { get; set; }
        public int ExperienceCount { get; set; }
        public int TrainingCount { get; set; }
        public int PublicationCount { get; set; }
        public List<RecentItemVM> DeptNotices { get; set; } = new();
    }

    // ── CONTENT EDITOR ────────────────────────────────────
    public class ContentEditorDashboardVM
    {
        public int PageId { get; set; }  // 0 = not assigned
        public string PageTitle { get; set; } = string.Empty;
        public string PageSlug { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime? LastUpdated { get; set; }
        //public int NewsCount { get; set; }
        //public int ContentPageCount { get; set; }
        //public int ActivityCount { get; set; }
        //public int AchievementCount { get; set; }
        //public int MarqueeCount { get; set; }
        //public List<RecentItemVM> RecentNews { get; set; } = new();
        //public List<RecentItemVM> RecentPages { get; set; } = new();
    }

    // ── PLACEMENT OFFICER ─────────────────────────────────
    public class PlacementDashboardVM
    {
        public int StatCount { get; set; }
        public int TeamCount { get; set; }
        public int RecruiterCount { get; set; }
        public string LatestYear { get; set; } = string.Empty;
        public int LatestTotalPlaced { get; set; }
        public int LatestTotalStudents { get; set; }
        public string LatestHighestPkg { get; set; } = string.Empty;
        public string LatestAveragePkg { get; set; } = string.Empty;
        public List<RecentItemVM> AllStats { get; set; } = new();
    }

    // ── COMMITTEE HEAD ────────────────────────────────────
    public class CommitteeHeadDashboardVM
    {
        public int CommitteeId { get; set; }
        public string CommitteeTitle { get; set; } = string.Empty;
        public string? CommitteeTagline { get; set; }
        public int MemberCount { get; set; }
        public int ActivityCount { get; set; }
        public List<RecentItemVM> RecentActivities { get; set; } = new();
    }
}