namespace GECPatan.Web.Models.Dtos
{
    // Mirrors GECPatan.Api.DTOs.MenuItemDTO exactly.
    // NOTE: the API already resolves the final URL server-side (Link),
    // there is no ControllerName/ActionName/dynamicID on this DTO —
    // the old MenuVM shape doesn't apply here.
    public class MenuItemDTO
    {
        public int Id { get; set; }
        public string MenuText { get; set; } = string.Empty;
        public string LinkType { get; set; } = "none"; // "internal" | "dynamic" | "external" | "none"
        public string? Link { get; set; }              // resolved final URL (null if "none")
        public string? CssClass { get; set; }
        public int Position { get; set; }
        public bool OpenInNewTab { get; set; }

        public List<MenuItemDTO> Children { get; set; } = new();
    }
}
