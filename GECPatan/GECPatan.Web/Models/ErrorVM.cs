namespace GECPatan.Web.Models
{
    /// <summary>
    /// Drives the single shared error view (Views/Error/Index.cshtml).
    /// One model, one view — the copy and which call-to-action buttons show
    /// up (Home / Contact Us) change per status code, but the layout stays
    /// the same, so there's one place to update the error page's design.
    /// </summary>
    public class ErrorViewModel
    {
        public int StatusCode { get; set; }
        public string Title { get; set; } = "Something went wrong";
        public string Message { get; set; } = "An unexpected error occurred.";

        public bool ShowGoHome { get; set; } = true;
        public bool ShowContactUs { get; set; }

        /// <summary>
        /// Correlates what the visitor sees to what's in the server logs,
        /// without ever showing them the actual exception message or stack
        /// trace.
        /// </summary>
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}