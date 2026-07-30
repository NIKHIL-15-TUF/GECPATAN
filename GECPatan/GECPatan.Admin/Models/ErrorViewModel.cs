namespace GECPatan.Admin.Models
{
    /// <summary>
    /// Drives the single shared error view (Views/Error/Index.cshtml).
    /// Extended from the original RequestId-only model — those two members
    /// are unchanged, so nothing else referencing them breaks.
    /// </summary>
    public class ErrorViewModel
    {
        public int StatusCode { get; set; }
        public string Title { get; set; } = "Something went wrong";
        public string Message { get; set; } = "An unexpected error occurred.";
        public bool ShowGoToDashboard { get; set; } = true;

        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}