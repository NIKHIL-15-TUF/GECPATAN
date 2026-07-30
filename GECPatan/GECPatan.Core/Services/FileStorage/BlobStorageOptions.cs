namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Bound from the "BlobStorage" section of appsettings.json. See Program.cs
    /// for how this feeds into the BlobStorageService DI registration.
    /// </summary>
    public class BlobStorageOptions
    {
        public const string SectionName = "BlobStorage";

        /// <summary>
        /// Azure Storage connection string. For local development against
        /// Azurite (see docker-compose.yml), use the well-known shorthand
        /// "UseDevelopmentStorage=true" — the Azure SDK expands this
        /// automatically to Azurite's default local endpoints and well-known
        /// devstoreaccount1 key, so no real credentials are ever needed locally.
        /// </summary>
        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

        /// <summary>Blob container that all uploads are written into. Created automatically if it doesn't exist.</summary>
        public string ContainerName { get; set; } = "uploads";

        /// <summary>
        /// Optional override for the base URL handed back to callers/browsers
        /// (e.g. a CDN, reverse proxy, or production Azure Storage account
        /// custom domain). If left null, the container's own endpoint URL —
        /// derived directly from ConnectionString — is used as-is. For
        /// Azurite that resolves to http://127.0.0.1:10000/devstoreaccount1.
        /// </summary>
        public string? PublicBaseUrl { get; set; }
    }
}
