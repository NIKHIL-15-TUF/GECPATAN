namespace GECPatan.Api.DTOs
{
    // ── ACADEMIC CALENDAR (for /api/academics/calendar) ────
    public class AcademicCalendarDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? UploadDate { get; set; }
    public int? DeptId { get; set; }
    public string? DeptName { get; set; }
    public int DisplayOrder { get; set; }
}

// ── SSIP DOCUMENT (for /api/academics/ssip) ────────────
public class SSIPDocumentDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? UploadDate { get; set; }
    public int DisplayOrder { get; set; }
}

// ── RESEARCH GRANT (for /api/academics/research) ───────
public class ResearchGrantDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PrincipalInvestigator { get; set; }
    public string? StartDate { get; set; }
    public string? CompletionDate { get; set; }
    public string? Duration { get; set; }
    public string? ProjectCost { get; set; }
    public string? SponsoringAuthority { get; set; }
    public int DisplayOrder { get; set; }
}
}