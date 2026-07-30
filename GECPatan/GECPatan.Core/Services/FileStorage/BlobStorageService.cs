using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Blob-storage-backed implementation of IFileStorageService, targeting
    /// Azurite locally (see docker-compose.yml at the repo root) and a real
    /// Azure Storage account in production via the same connection-string
    /// based configuration.
    ///
    /// This exists alongside FileStorageService (disk-based, wwwroot/uploads)
    /// without modifying it in any way — the two are fully independent
    /// implementations of the same interface. Only the DI registration in
    /// Program.cs decides which one is active; every controller depends on
    /// IFileStorageService and needs no changes either way.
    ///
    /// Validation performed for every upload — identical rules to
    /// FileStorageService, deliberately duplicated rather than shared so this
    /// class has zero coupling to (and makes zero changes to) the existing
    /// implementation:
    ///   1. File present and non-empty
    ///   2. File size within the per-category limit
    ///   3. Extension is in the per-category whitelist
    ///   4. Browser-declared Content-Type is in the per-category whitelist
    ///   5. The file's actual bytes (magic number / signature) match a known
    ///      format for the category.
    ///
    /// Only after all five checks pass is anything uploaded, and the blob is
    /// always written under a fresh GUID name with the *validated* extension —
    /// the original filename is never trusted or persisted.
    /// </summary>
    public class BlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _publicBaseUrl;
        private readonly ILogger<BlobStorageService> _logger;

        private readonly SemaphoreSlim _containerInitLock = new(1, 1);
        private volatile bool _containerEnsured;

        // Folder names are restricted to this pattern to prevent path
        // traversal / blob-name injection via a crafted "folder" argument.
        private static readonly Regex ValidFolderName = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private static readonly Dictionary<FileCategory, CategoryRule> Rules = new()
        {
            [FileCategory.Image] = new CategoryRule(
                MaxSizeBytes: 5 * 1024 * 1024, // 5 MB
                Extensions: new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                ContentTypes: new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
                ContentTypeHeader: static ext => ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                },
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
                ContentTypeHeader: static ext => ext switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                },
                SignatureCheck: IsValidDocumentSignature),
        };

        /// <param name="connectionString">
        /// Azure Storage connection string — for local dev, Azurite's
        /// well-known "UseDevelopmentStorage=true" shorthand.
        /// </param>
        /// <param name="containerName">Container to upload into (auto-created if missing).</param>
        /// <param name="publicBaseUrl">
        /// Optional override for the base URL returned to callers. When null,
        /// the container client's own endpoint URL is used directly.
        /// </param>
        public BlobStorageService(
            string connectionString,
            string containerName,
            string? publicBaseUrl,
            ILogger<BlobStorageService> logger)
        {
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _publicBaseUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
                ? _containerClient.Uri.ToString().TrimEnd('/')
                : publicBaseUrl!.TrimEnd('/') + "/" + containerName;
            _logger = logger;
        }

        private async Task EnsureContainerAsync()
        {
            if (_containerEnsured) return;

            await _containerInitLock.WaitAsync();
            try
            {
                if (_containerEnsured) return;
                await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                _containerEnsured = true;
            }
            finally
            {
                _containerInitLock.Release();
            }
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
            // and upload it without asking the client for the bytes twice.
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

            try
            {
                await EnsureContainerAsync();

                var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
                var blobClient = _containerClient.GetBlobClient(blobName);

                memoryStream.Position = 0;
                await blobClient.UploadAsync(memoryStream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = rule.ContentTypeHeader(extension) }
                });

                // Not a filesystem-relative path like FileStorageService returns —
                // it's a full, directly-usable URL. Every view/consumer already
                // treats an http(s)-prefixed path as-is (see DocumentsController /
                // Category.cshtml / TableView.cshtml ResolveApiFileUrl helpers),
                // so this is a drop-in value for the same FileUploadResult.RelativePath.
                return FileUploadResult.Ok($"{_publicBaseUrl}/{blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload blob for folder '{Folder}'", folder);
                return FileUploadResult.Fail("Unable to save the file right now. Please try again.");
            }
        }

        public bool Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var blobName = ExtractBlobName(relativePath);
            if (blobName is null)
            {
                _logger.LogWarning("Refused to delete path outside the configured container: {Path}", relativePath);
                return false;
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                return blobClient.DeleteIfExists();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete blob for path {Path}", relativePath);
                return false;
            }
        }

        /// <summary>
        /// Accepts either a full URL previously handed back by SaveAsync
        /// (e.g. "http://127.0.0.1:10000/devstoreaccount1/uploads/gallery/xxx.jpg")
        /// or a bare blob name ("gallery/xxx.jpg"), as defense in depth against
        /// hand-edited values or records predating this service. Returns null —
        /// causing Delete() to refuse — for anything that looks like it points
        /// somewhere else entirely (a different host/URL).
        /// </summary>
        private string? ExtractBlobName(string relativePath)
        {
            var prefix = _publicBaseUrl + "/";
            if (relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return relativePath[prefix.Length..];

            if (!relativePath.Contains("://"))
                return relativePath.TrimStart('/');

            return null;
        }

        // ── Magic-byte / file-signature checks ──────────────────────────
        // Identical rules to FileStorageService — these look at the first few
        // bytes of the actual file content, which cannot be spoofed just by
        // renaming a file or lying about its Content-Type header.

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
            Func<string, string> ContentTypeHeader,
            Func<byte[], bool> SignatureCheck);
    }
}
