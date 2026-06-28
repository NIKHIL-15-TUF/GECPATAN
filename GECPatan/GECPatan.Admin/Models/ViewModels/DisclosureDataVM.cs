
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
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
    public class DisclosurePrincipalVM
    {
        public string Name { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public List<string> Qualifications { get; set; } = new();

        public string? Email { get; set; }

        public string? Contact { get; set; }
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

    public class DisclosureDataVM
    {
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        // Section 1 — About (TinyMCE)
        public string? AboutInstitute { get; set; }

        // Section 2 — Institute Info
        public InstituteInfoVM Institute { get; set; } = new();

        // Section 3 — Governance narratives
        public string? GovernanceHtml { get; set; }
        public List<NarrativeSectionVM> GovernanceNarratives { get; set; } = new();

        // Section 4 — Programs
        public List<NBAAccreditationVM> NBAAccreditations { get; set; } = new();
        public List<DisclosureDeptProgramVM> Programs { get; set; } = new();
        public string? AccreditationStatusHtml { get; set; }

        // Section 5 — Cutoffs (per dept)
        public List<DisclosureDeptVM> DeptCutoffs { get; set; } = new();

        // Section 6 — Placement
        public string? PlacementFacilitiesHtml { get; set; }
        public List<DisclosurePlacementRowVM> PlacementData { get; set; } = new();
        public string? ForeignCollaborationHtml { get; set; }

        // Section 7 — Faculty
        public DisclosurePrincipalVM? Principal { get; set; }
        public List<DisclosureDeptFacultyVM> DeptFaculty { get; set; } = new();

        // Section 8 — Fee
        public string? FeeStructureHtml { get; set; }

        // Section 9 — Admission
        public string? AdmissionProcessHtml { get; set; }
        public string? CriteriaWeightagesHtml { get; set; }
        public string? ApplicantListHtml { get; set; }
        public string? ManagementSeatsHtml { get; set; }

        // Section 10 — Infrastructure
        public List<InfrastructureRowVM> Classrooms { get; set; } = new();
        public List<InfrastructureRowVM> TutorialRooms { get; set; } = new();
        public List<InfrastructureRowVM> Laboratories { get; set; } = new();
        public List<InfrastructureRowVM> DrawingHalls { get; set; } = new();
        public List<InfrastructureRowVM> OtherInfra { get; set; } = new();
        public string? InfrastructureInfoHtml { get; set; }

        // Section 11 — Library
        public string? LibraryInfoHtml { get; set; }

        // Section 12 — Department Equipment
        public List<DeptEquipmentVM> DeptEquipments { get; set; } = new();

        // Section 13 — Best Practices
        public string? BestPracticesHtml { get; set; }

        // Warnings
        public List<string> EmptyNarrativeWarnings { get; set; } = new();
    }

    // ── Sub VMs ──────────────────────────────────────────────
    public class NBAAccreditationVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Program name is required")]
        [Display(Name = "Program Name")]
        public string ProgramName { get; set; } = string.Empty;

        [Display(Name = "Accredited By")]
        public string? AccreditedBy { get; set; } = "NBA";

        [Display(Name = "Valid From (e.g. 2023)")]
        public string? ValidFrom { get; set; }

        [Display(Name = "Valid To (e.g. 2026)")]
        public string? ValidTo { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; } = "Accredited";

        [Display(Name = "Department (optional)")]
        public int? DeptId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem("Accredited",   "Accredited"),
            new SelectListItem("Applied",      "Applied"),
            new SelectListItem("Not Applied",  "Not Applied"),
            new SelectListItem("Expired",      "Expired"),
            new SelectListItem("Eligible",     "Eligible"),
            new SelectListItem("Not Eligible", "Not Eligible")
        };
    }
    public class DisclosureDeptProgramVM
    {
        public string DeptName { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public int Intake { get; set; }
        public int IntakeYear { get; set; }
        public string Duration { get; set; } = "4 Years";
        public string Level { get; set; } = "UG";
    }

    public class DisclosurePlacementRowVM
    {
        public string AcademicYear { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalPlaced { get; set; }
        public string? HighestPkg { get; set; }
        public string? AveragePkg { get; set; }
        public string? TopRecruiter { get; set; }
    }

    public class DisclosureDeptFacultyVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public List<FacultySummaryRowVM> SummaryTable { get; set; } = new();
        public List<DisclosureFacultyDetailVM> Faculty { get; set; } = new();
    }

    public class FacultySummaryRowVM
    {
        public int SrNo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Post { get; set; } = string.Empty;
        public string? ApprovalStatus { get; set; }
        public string? LetterNumber { get; set; }
    }

    public class DisclosureFacultyDetailVM
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DateOfBirth { get; set; }
        public string Qualifications { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? ImagePath { get; set; }
        public int TeachingYears { get; set; }
        public List<ExperienceRowVM> Experiences { get; set; } = new();
        public List<string> UGSubjects { get; set; } = new();
        public List<string> PGSubjects { get; set; } = new();
        public int MastersOngoing { get; set; }
        public int MastersCompleted { get; set; }
        public int PhDOngoing { get; set; }
        public int PhDCompleted { get; set; }
        public List<PublicationRowVM> Publications { get; set; } = new();
        public int ConsultancyCount { get; set; }
        public List<PatentRowVM> Patents { get; set; } = new();
        public List<BookRowVM> Books { get; set; } = new();
        public List<string> Trainings { get; set; } = new();
        public List<string> Seminars { get; set; } = new();
        public List<MembershipRowVM> Memberships { get; set; } = new();
    }

    public class ExperienceRowVM
    {
        public string Organization { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }

    public class PublicationRowVM
    {
        public int SrNo { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Journal { get; set; }
        public string? Year { get; set; }
        public string? CoAuthors { get; set; }
    }

    public class PatentRowVM
    {
        public string Title { get; set; } = string.Empty;
        public string? ApplicationNo { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BookRowVM
    {
        public int SrNo { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Publisher { get; set; }
        public string? Year { get; set; }
    }

    public class MembershipRowVM
    {
        public string Community { get; set; } = string.Empty;
        public string MembershipType { get; set; } = string.Empty;
    }

    public class DeptEquipmentVM
    {
        public string DeptName { get; set; } = string.Empty;
        public string? EquipmentHtml { get; set; }
    }
    public class DeptEquipmentEditVM
    {
        public int Id { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string? EquipmentHtml { get; set; }
    }

    public class DeptEquipmentListVM
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public bool HasContent { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

