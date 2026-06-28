using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    // CUTOFF RECORD — per dept, per year, per category
    // Matches AICTE Mandatory Disclosure cutoff table
    public class CutoffRecord : BaseEntity
    {
        public int Id { get; set; }

        public int DeptId { get; set; }

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }

        // e.g. "2025-26"
        [Required, MaxLength(20)]
        public string AcademicYear { get; set; } = string.Empty;

        // Ranks — null = category not applicable / no admission
        public int? GeneralRank { get; set; }
        public int? SEBCRank { get; set; }
        public int? SCRank { get; set; }
        public int? STRank { get; set; }
        public int? EWSRank { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // SCHOLARSHIP RECORD — per scheme, per year
    public class ScholarshipRecord : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string SchemeName { get; set; } = string.Empty;

        // e.g. "2025-26"
        [MaxLength(20)]
        public string? AcademicYear { get; set; }

        public int TotalApplications { get; set; } = 0;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // INFRASTRUCTURE RECORD — room counts/areas
    // Category: Classroom / Tutorial / Laboratory /
    //           DrawingHall / Other
    public class InfrastructureRecord : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string RoomType { get; set; } = string.Empty;
        // "Classroom", "Tutorial Room", "Laboratory",
        // "Drawing Hall", "Computer Center", "Library & Reading Room"

        public double AreaSqm { get; set; } = 0;

        [MaxLength(200)]
        public string? BuildingName { get; set; }
        // e.g. "Civil Engineering Department"

        public int? DeptId { get; set; }

        [ForeignKey("DeptId")]
        public Department? Department { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // DISCLOSURE NARRATIVE — fixed-key rich text blocks
    // STRICT SCHEMA: only these SectionKey values allowed
    // (enforced in controller, not free-form)
    public class DisclosureNarrative : BaseEntity
    {
        public int Id { get; set; }

        // One of DisclosureSectionKeys constants below.
        // Unique — one narrative block per key.
        [Required, MaxLength(100)]
        public string SectionKey { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string SectionTitle { get; set; } = string.Empty;

        // Rich text (TinyMCE) — admin-editable content
        public string? HtmlContent { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public DateTime? LastUpdated { get; set; }

        [MaxLength(200)]
        public string? UpdatedBy { get; set; }
    }
    // ── NBA ACCREDITATION TABLE ──────────────────────────────
    public class NBAAccreditation : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string ProgramName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? AccreditedBy { get; set; } // NBA / NAAC etc.

        [MaxLength(20)]
        public string? ValidFrom { get; set; }

        [MaxLength(20)]
        public string? ValidTo { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; } // Accredited / Applied / Not Applied

        public int? DeptId { get; set; }
        public Department? Department { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── PLACEMENT DATA (per year) ─────────────────────────────
    public class DisclosurePlacementData : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string AcademicYear { get; set; } = string.Empty;

        public int TotalStudents { get; set; } = 0;
        public int TotalPlaced { get; set; } = 0;

        [MaxLength(100)]
        public string? HighestPackage { get; set; }

        [MaxLength(100)]
        public string? AveragePackage { get; set; }

        [MaxLength(100)]
        public string? TopRecruiter { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }

    // ── FACULTY APPROVAL INFO ─────────────────────────────────
    // Extra fields for Mandatory Disclosure faculty table
    // (Appointment approved by university, letter number)
    public class FacultyApprovalInfo : BaseEntity
    {
        public int Id { get; set; }

        public int FacultyId { get; set; }

        // Approval Status: Approved / Pending / NA
        [MaxLength(50)]
        public string ApprovalStatus { get; set; } = "Approved";

        [MaxLength(200)]
        public string? ApprovalLetterNumber { get; set; }

        [MaxLength(200)]
        public string? ApprovedByUniversity { get; set; }
    }

    // ── DEPARTMENT EQUIPMENT (TinyMCE per dept) ───────────────
    public class DepartmentEquipment : BaseEntity
    {
        public int Id { get; set; }

        public int DeptId { get; set; }

        // TinyMCE content for dept's lab/equipment list
        public string? EquipmentHtml { get; set; }

        public DateTime? LastUpdated { get; set; }

        [MaxLength(200)]
        public string? UpdatedBy { get; set; }
    }
    // STRICT SCHEMA — allowed narrative section keys
    // Admin can EDIT content for these keys only.
    // Cannot add arbitrary new sections (per requirement:
    // "strict schema is also important")
    public static class DisclosureSectionKeys
    {
        // ABOUT (new - TinyMCE direct)
        public const string AboutInstitute = "AboutInstitute";

        // GOVERNANCE
        public const string Governance = "Governance";
        public const string AcademicAdvisoryBody = "AcademicAdvisoryBody";
        public const string BoardMeetings = "BoardMeetings";
        public const string OrganizationalChart = "OrganizationalChart";
        public const string FacultyStudentInvolvement = "FacultyStudentInvolvement";
        public const string GovernanceMechanism = "GovernanceMechanism";
        public const string StudentFeedback = "StudentFeedback";
        public const string GrievanceRedressal = "GrievanceRedressal";

        // PROGRAMS (new narrative sections)
        public const string AccreditationStatus = "AccreditationStatus";

        // PLACEMENT
        public const string PlacementFacilities = "PlacementFacilities";
        public const string ForeignCollaboration = "ForeignCollaboration";

        // FEE
        public const string FeeStructure = "FeeStructure";

        // ADMISSION
        public const string AdmissionProcess = "AdmissionProcess";
        public const string CriteriaWeightages = "CriteriaWeightages";
        public const string ApplicantList = "ApplicantList";
        public const string ManagementSeatsResult = "ManagementSeatsResult";

        // INFRASTRUCTURE
        public const string InfrastructureInfo = "InfrastructureInfo";

        // LIBRARY (new - TinyMCE direct)
        public const string LibraryInfo = "LibraryInfo";

        // BEST PRACTICES (new)
        public const string BestPractices = "BestPractices";

        public static readonly string[] AllKeys = new[]
        {
            AboutInstitute,
            Governance, AcademicAdvisoryBody,BoardMeetings, OrganizationalChart,
            FacultyStudentInvolvement, GovernanceMechanism,
            StudentFeedback, GrievanceRedressal,
            AccreditationStatus,
            PlacementFacilities, ForeignCollaboration,
            FeeStructure,
            AdmissionProcess, CriteriaWeightages,
            ApplicantList, ManagementSeatsResult,
            InfrastructureInfo,
            LibraryInfo,
            BestPractices
        };

        public static readonly Dictionary<string, string> DefaultTitles = new()
        {
            [AboutInstitute] = "About the Institute",
            [Governance] = "Governance",
            [AcademicAdvisoryBody] = "Members of Academic Advisory Body",
            [BoardMeetings]= "Frequency of the Board Meetings and Academic Advisory Body",
            [OrganizationalChart] = "Organizational Chart and Processes",
            [FacultyStudentInvolvement] = "Faculty & Student Involvement",
            [GovernanceMechanism] = "Mechanism for Good Governance",
            [StudentFeedback] = "Student Feedback on Governance",
            [GrievanceRedressal] = "Grievance Redressal Mechanism",
            [AccreditationStatus] = "Accreditation Status",
            [PlacementFacilities] = "Placement Facilities",
            [ForeignCollaboration] = "Foreign Collaboration Details",
            [FeeStructure] = "Fee Structure",
            [AdmissionProcess] = "Admission Process",
            [CriteriaWeightages] = "Criteria & Weightages for Admission",
            [ApplicantList] = "List of Applicants",
            [ManagementSeatsResult] = "Result of Admission under Management/Vacant Seats",
            [InfrastructureInfo] = "Additional Infrastructural Information",
            [LibraryInfo] = "Library Facilities",
            [BestPractices] = "Best Practices Adopted"
        };

        // Display titles for section headers in document
        public static readonly Dictionary<string, string> SectionGroupTitles = new()
        {
            [AboutInstitute] = "About the Institute",
            [Governance] = "Governance",
            [AccreditationStatus] = "Programs",
            [PlacementFacilities] = "Placement",
            [FeeStructure] = "Fee Structure",
            [AdmissionProcess] = "Admission",
            [InfrastructureInfo] = "Infrastructure",
            [LibraryInfo] = "Library",
            [BestPractices] = "Best Practices"
        };
    }
}