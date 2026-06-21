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

    // STRICT SCHEMA — allowed narrative section keys
    // Admin can EDIT content for these keys only.
    // Cannot add arbitrary new sections (per requirement:
    // "strict schema is also important")
    public static class DisclosureSectionKeys
    {
        public const string Governance = "Governance";
        public const string AcademicAdvisoryBody = "AcademicAdvisoryBody";
        public const string OrganizationalChart = "OrganizationalChart";
        public const string FacultyStudentInvolvement = "FacultyStudentInvolvement";
        public const string GovernanceMechanism = "GovernanceMechanism";
        public const string StudentFeedback = "StudentFeedback";
        public const string GrievanceRedressal = "GrievanceRedressal";
        public const string ForeignCollaboration = "ForeignCollaboration";
        public const string AdmissionProcedure = "AdmissionProcedure";
        public const string AdmissionCriteria = "AdmissionCriteria";
        public const string FeeWaiverPolicy = "FeeWaiverPolicy";
        public const string HostelFacilities = "HostelFacilities";
        public const string LibraryFacilities = "LibraryFacilities";
        public const string ComputingFacilities = "ComputingFacilities";
        public const string ExtraCurricularFacilities = "ExtraCurricularFacilities";

        public static readonly string[] AllKeys = new[]
        {
            Governance, AcademicAdvisoryBody, OrganizationalChart,
            FacultyStudentInvolvement, GovernanceMechanism,
            StudentFeedback, GrievanceRedressal, ForeignCollaboration,
            AdmissionProcedure, AdmissionCriteria, FeeWaiverPolicy,
            HostelFacilities, LibraryFacilities, ComputingFacilities,
            ExtraCurricularFacilities
        };

        public static readonly Dictionary<string, string> DefaultTitles = new()
        {
            [Governance] = "Governance",
            [AcademicAdvisoryBody] = "Members of Academic Advisory Body",
            [OrganizationalChart] = "Organizational Chart and Processes",
            [FacultyStudentInvolvement] = "Faculty & Student Involvement in Academic Affairs",
            [GovernanceMechanism] = "Mechanism/Norms & Procedure for Good Governance",
            [StudentFeedback] = "Student Feedback on Institutional Governance",
            [GrievanceRedressal] = "Grievance Redressal Mechanism",
            [ForeignCollaboration] = "Foreign Collaboration Details",
            [AdmissionProcedure] = "Admission Procedure",
            [AdmissionCriteria] = "Criteria and Weightages for Admission",
            [FeeWaiverPolicy] = "Fee Waiver / Scholarship Policy",
            [HostelFacilities] = "Hostel Facilities",
            [LibraryFacilities] = "Library Facilities",
            [ComputingFacilities] = "Computing Facilities",
            [ExtraCurricularFacilities] = "Extra-Curricular Facilities"
        };
    }
}