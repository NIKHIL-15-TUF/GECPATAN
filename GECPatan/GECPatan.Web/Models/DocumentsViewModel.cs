using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class DocumentCategoryListViewModel
    {
        public List<DocumentCategoryListDTO> Categories { get; set; } = new();
        public int MoUCount { get; set; }
        public int SSIPCount { get; set; }
        public int TimetableCount { get; set; }
        public string? ApiBaseUrl { get; set; }

        public bool HasAnyDocuments =>
            Categories.Any() || MoUCount > 0 || SSIPCount > 0 || TimetableCount > 0;
    }

    public class DocumentCategoryDetailViewModel
    {
        public DocumentCategoryDetailDTO? Category { get; set; }
        public string? ApiBaseUrl { get; set; }
    }

    public class SimpleDocumentRow
    {
        public string Title { get; set; } = string.Empty;
        public string SubLabel { get; set; } = string.Empty;  // e.g. Month/Year, Upload Date, Semester
        public string? FilePath { get; set; }
    }

    public class SimpleDocumentListViewModel
    {
        public string PageTitle { get; set; } = string.Empty;
        public string SubLabelHeader { get; set; } = "Date";
        public List<SimpleDocumentRow> Rows { get; set; } = new();
        public string? ApiBaseUrl { get; set; }
    }
}
