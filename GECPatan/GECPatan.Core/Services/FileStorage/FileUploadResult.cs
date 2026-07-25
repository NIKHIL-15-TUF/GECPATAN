namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Result of a file upload attempt. Callers must check Success before
    /// using RelativePath — this forces controllers to handle validation
    /// failures (bad extension, oversized file, etc.) instead of silently
    /// saving whatever was sent, which is what the old per-controller
    /// SaveFileAsync() methods did.
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; init; }

        /// <summary>Web-relative path (e.g. "/uploads/gallery/xxxx.jpg") when Success == true.</summary>
        public string? RelativePath { get; init; }

        /// <summary>User-friendly reason for failure when Success == false.</summary>
        public string? ErrorMessage { get; init; }

        public static FileUploadResult Ok(string relativePath) =>
            new() { Success = true, RelativePath = relativePath };

        public static FileUploadResult Fail(string errorMessage) =>
            new() { Success = false, ErrorMessage = errorMessage };
    }
}
