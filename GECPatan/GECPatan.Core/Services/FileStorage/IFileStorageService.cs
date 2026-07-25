using Microsoft.AspNetCore.Http;

namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Single, centralized entry point for saving and deleting user-uploaded
    /// files. This replaces the ~26 hand-copied SaveFileAsync/DeleteFile pairs
    /// that used to live directly inside individual Admin controllers with no
    /// validation. Every controller that accepts an upload should depend on
    /// this interface instead of writing its own file-handling code.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Validates and saves an uploaded file under wwwroot/uploads/{folder}/.
        /// Returns a failed result (never throws for bad input) if the file is
        /// missing, oversized, has a disallowed extension/content-type, or
        /// fails the magic-byte signature check for its declared category.
        /// </summary>
        /// <param name="file">The posted file (may be null/empty — treated as a validation failure, not an exception).</param>
        /// <param name="folder">Sub-folder under wwwroot/uploads, e.g. "gallery", "departments". Must be a simple folder name (letters, numbers, hyphen, underscore only).</param>
        /// <param name="category">Which validation rule set to apply.</param>
        Task<FileUploadResult> SaveAsync(IFormFile? file, string folder, FileCategory category);

        /// <summary>
        /// Deletes a previously-saved file given its web-relative path
        /// (e.g. "/uploads/gallery/xxxx.jpg"). Safe to call with null/empty/
        /// already-deleted paths — this is a no-op, not an error, matching
        /// the behavior of the original per-controller DeleteFile() helpers.
        /// Returns false (and logs a warning) if the resolved path would fall
        /// outside the uploads root — this should never legitimately happen
        /// since paths are always server-generated, but it is checked anyway
        /// as defense in depth.
        /// </summary>
        bool Delete(string? relativePath);
    }
}
