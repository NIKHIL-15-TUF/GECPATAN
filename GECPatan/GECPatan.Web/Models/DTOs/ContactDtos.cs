namespace GECPatan.Web.Models.Dtos
{
    public class ContactSettingsDTO
    {
        public string? CollegeName { get; set; }
        public string? Address { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Email1 { get; set; }
        public string? Email2 { get; set; }
        public string? OfficeHours { get; set; }
        public string? MapEmbedUrl { get; set; }
        public List<string> AllowedCategories { get; set; } = new();
    }

    // Sent as the body of POST api/contact/submit
    public class ContactSubmitRequestDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
