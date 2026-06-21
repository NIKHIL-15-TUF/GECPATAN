namespace GECPatan.Admin.Models.ViewModels
{
    // ── INDEX — list of all 15 fixed sections ──────────────
    public class DisclosureNarrativeListVM
    {
        public int Id { get; set; }
        public string SectionKey { get; set; } = string.Empty;
        public string SectionTitle { get; set; } = string.Empty;
        public bool HasContent { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? UpdatedBy { get; set; }
    }

    // ── EDIT FORM ────────────────────────────────────────────
    public class DisclosureNarrativeEditVM
    {
        public int Id { get; set; }

        // Read-only display — cannot be changed (strict schema)
        public string SectionKey { get; set; } = string.Empty;
        public string SectionTitle { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}