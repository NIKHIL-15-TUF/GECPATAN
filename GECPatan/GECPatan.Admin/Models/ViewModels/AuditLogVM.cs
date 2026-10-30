namespace GECPatan.Admin.Models.ViewModels
{
    public class AuditChangeVM
    {
        public string Field { get; set; } = string.Empty;
        public string? Old { get; set; }
        public string? New { get; set; }
    }

    public class AuditLogListVM
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Module { get; set; }
        public int? RecordId { get; set; }
        public string? RecordName { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? Source { get; set; }
        public string? RequestPath { get; set; }

        // Parsed from the stored JSON
        public List<AuditChangeVM> Changes { get; set; } = new();   // Updated: old -> new
        public List<AuditChangeVM> Values { get; set; } = new();    // Created / Deleted: snapshot
        public string? DetailsText { get; set; }                    // free text / note

        public bool HasDetails => Changes.Any() || Values.Any() || !string.IsNullOrEmpty(DetailsText);
    }

    public class AuditLogFilterVM
    {
        public string? Search { get; set; }       // user name or record name
        public string? Module { get; set; }
        // NOT called "Action": MVC would fill that from the route value action=Index
        public string? ActionType { get; set; }
        public string? Source { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;

        public List<AuditLogListVM> Logs { get; set; } = new();
        public List<string> Modules { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 50;

        // last-24-hours summary tiles
        public int Created24h { get; set; }
        public int Updated24h { get; set; }
        public int Deleted24h { get; set; }
        public int ActiveUsers24h { get; set; }
    }
}