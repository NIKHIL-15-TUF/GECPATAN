namespace GECPatan.Api.DTOs
{
    // ── MENU ITEM (recursive tree, for /api/menu/main, /footer) ─
    public class MenuItemDTO
    {
        public int Id { get; set; }
        public string MenuText { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none";
        public string? Link { get; set; } // resolved final URL (null if "none")
        public string? CssClass { get; set; }
        public int Position { get; set; }
        public bool OpenInNewTab { get; set; }

        public List<MenuItemDTO> Children { get; set; } = new();
    }

    // ── CONTENT PAGE (for /api/pages/{slug}) ───────────────
    public class ContentPageDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }

        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }
}