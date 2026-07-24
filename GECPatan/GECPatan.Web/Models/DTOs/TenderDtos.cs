namespace GECPatan.Web.Models.Dtos
{
    public class TenderGroupDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<TenderDocumentDTO> Documents { get; set; } = new();
    }

    public class TenderDocumentDTO
    {
        public int Id { get; set; }
        public string DocTitle { get; set; } = string.Empty;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? MonthYear { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
    }
}
