using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Default implementation of IFileStorageService.
    ///
    /// Validation performed for every upload, in order:
    ///   1. File present and non-empty
    ///   2. File size within the per-category limit
    ///   3. Extension is in the per-category whitelist
    ///   4. Browser-declared Content-Type is in the per-category whitelist
    ///   5. The file's actual bytes (magic number / signature) match a known
    ///      format for the category — this is the important one, because
    ///      steps 3 and 4 are both just trusting labels the client sent us.
    ///      A file renamed "photo.jpg" that is actually an HTML/script payload
    ///      will fail this check even though it passes the first two.
    ///
    /// Only after all five checks pass is anything written to disk, and the
    /// file is always written under a fresh GUID name with the *validated*
    /// extension — the original filename is never trusted or persisted.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly string _webRootPath;
        private readonly ILogger<FileStorageService> _logger;

        // Folder names are restricted to this pattern to prevent path
        // traversal via a crafted "folder" argument (e.g. "../../wwwroot").
        private static readonly Regex ValidFolderName = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private static readonly Dictionary<FileCategory, CategoryRule> Rules = new()
        {
            [FileCategory.Image] = new CategoryRule(
                MaxSizeBytes: 5 * 1024 * 1024, // 5 MB
                Extensions: new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                ContentTypes: new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
                SignatureCheck: IsValidImageSignature),

            [FileCategory.Document] = new CategoryRule(
                MaxSizeBytes: 10 * 1024 * 1024, // 10 MB
                Extensions: new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" },
                ContentTypes: new[]
                {
                    "application/pdf",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/vnd.ms-excel",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                },
                SignatureCheck: IsValidDocumentSignature),
        };

        public FileStorageService(string webRootPath, ILogger<FileStorageService> logger)
        {
            _webRootPath = webRootPath;
            _logger = logger;
        }

        public async Task<FileUploadResult> SaveAsync(IFormFile? file, string folder, FileCategory category)
        {
            if (file is null || file.Length == 0)
                return FileUploadResult.Fail("Please choose a file to upload.");

            if (!ValidFolderName.IsMatch(folder))
            {
                _logger.LogWarning("Rejected upload: invalid folder name '{Folder}'", folder);
                return FileUploadResult.Fail("Invalid upload destination.");
            }

            var rule = Rules[category];

            if (file.Length > rule.MaxSizeBytes)
            {
                return FileUploadResult.Fail(
                    $"File is too large. Maximum allowed size is {rule.MaxSizeBytes / (1024 * 1024)} MB.");
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!rule.Extensions.Contains(extension))
            {
                return FileUploadResult.Fail(
                    $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", rule.Extensions)}.");
            }

            if (!rule.ContentTypes.Contains(file.ContentType?.ToLowerInvariant() ?? ""))
            {
                return FileUploadResult.Fail("File content type does not match its extension.");
            }

            // Read the file into memory once so we can both signature-check it
            // and write it to disk without asking the client for the bytes twice.
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            if (!rule.SignatureCheck(bytes))
            {
                _logger.LogWarning(
                    "Rejected upload: file '{FileName}' failed signature check for category {Category}",
                    file.FileName, category);
                return FileUploadResult.Fail(
                    "This file's contents do not match a recognized, safe file format.");
            }

            var uploadsFolder = Path.Combine(_webRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes);

            return FileUploadResult.Ok($"/uploads/{folder}/{fileName}");
        }

        public bool Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var uploadsRoot = Path.Combine(_webRootPath, "uploads");
            var fullPath = Path.GetFullPath(Path.Combine(_webRootPath, relativePath.TrimStart('/')));

            // Defense in depth: refuse to delete anything outside wwwroot/uploads,
            // even though relativePath should only ever be a server-generated value.
            if (!fullPath.StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Refused to delete path outside uploads root: {Path}", relativePath);
                return false;
            }

            if (!File.Exists(fullPath))
                return false;

            File.Delete(fullPath);
            return true;
        }

        // ── Magic-byte / file-signature checks ──────────────────────────
        // These look at the first few bytes of the actual file content,
        // which cannot be spoofed just by renaming a file or lying about
        // its Content-Type header.

        private static bool IsValidImageSignature(byte[] bytes)
        {
            if (bytes.Length < 12) return false;

            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;

            // GIF: "GIF87a" or "GIF89a"
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) return true;

            // WEBP: "RIFF"....."WEBP"
            if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;

            return false;
        }

        private static bool IsValidDocumentSignature(byte[] bytes)
        {
            if (bytes.Length < 4) return false;

            // PDF: "%PDF"
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return true;

            // Legacy DOC/XLS (OLE Compound File): D0 CF 11 E0
            if (bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0) return true;

            // DOCX/XLSX (both are ZIP containers): PK 03 04
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04) return true;

            return false;
        }

        private record CategoryRule(
            long MaxSizeBytes,
            string[] Extensions,
            string[] ContentTypes,
            Func<byte[], bool> SignatureCheck);
    }
}
