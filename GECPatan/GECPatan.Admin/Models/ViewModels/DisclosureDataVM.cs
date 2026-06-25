namespace GECPatan.Admin.Models.ViewModels
{
    // ROOT VM — everything needed to generate the full
    // Mandatory Disclosure document (DOCX + PDF both use this)
    public class DisclosureDataVM
    {
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        public InstituteInfoVM Institute { get; set; } = new();
        public PrincipalSummaryVM? Principal { get; set; }

        public List<DisclosureDeptVM> Departments { get; set; } = new();

        public List<ScholarshipRowVM> Scholarships { get; set; } = new();
        public List<InfrastructureRowVM> Infrastructure { get; set; } = new();

        // 15 fixed narrative sections, in DisplayOrder
        public List<NarrativeSectionVM> Narratives { get; set; } = new();

        // Validation — sections with no content yet
        public List<string> EmptyNarrativeWarnings { get; set; } = new();
    }

    // ── INSTITUTE INFO (from SiteSettings) ─────────────────
    public class InstituteInfoVM
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? EstablishedYear { get; set; }
        public string? AffiliatedTo { get; set; } // university
        public string? ApprovedBy { get; set; } // AICTE
    }

    // ── PRINCIPAL SUMMARY (subset of full profile) ─────────
    public class PrincipalSummaryVM
    {
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public List<string> Qualifications { get; set; } = new();
    }

    // ── PER-DEPARTMENT BLOCK ────────────────────────────────
    public class DisclosureDeptVM
    {
        public int DeptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortCode { get; set; }

        public int CurrentIntake { get; set; }
        public int CurrentIntakeYear { get; set; }

        public List<DisclosureCutoffRowVM> CutoffHistory { get; set; } = new();
        public List<DisclosureFacultyVM> Faculty { get; set; } = new();
    }

    public class DisclosureCutoffRowVM
    {
        public string AcademicYear { get; set; } = string.Empty;
        public string General { get; set; } = "—";
        public string SEBC { get; set; } = "—";
        public string SC { get; set; } = "—";
        public string ST { get; set; } = "—";
        public string EWS { get; set; } = "—";
    }

    public class DisclosureFacultyVM
    {
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string HighestQualification { get; set; } = "—";
        public string DateOfJoining { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public int PublicationCount { get; set; }
    }

    // ── SCHOLARSHIP ROW ──────────────────────────────────────
    public class ScholarshipRowVM
    {
        public string SchemeName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
    }

    // ── INFRASTRUCTURE ROW ───────────────────────────────────
    public class InfrastructureRowVM
    {
        public string RoomType { get; set; } = string.Empty;
        public double AreaSqm { get; set; }
        public string BuildingName { get; set; } = "—";
        public string DeptName { get; set; } = "Institute-wide";
    }

    // ── NARRATIVE SECTION ─────────────────────────────────────
    public class NarrativeSectionVM
    {
        public string SectionKey { get; set; } = string.Empty;
        public string SectionTitle { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public int DisplayOrder { get; set; }
    }
}