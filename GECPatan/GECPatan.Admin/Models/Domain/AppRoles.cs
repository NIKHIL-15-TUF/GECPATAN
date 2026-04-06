namespace GECPatan.Admin.Models.Domain
{
    // All role name constants used across the application.
    // Always use these constants instead of hardcoded strings.
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Principal = "Principal";
        public const string HOD = "HOD";
        public const string Faculty = "Faculty";
        public const string ContentEditor = "ContentEditor";
        public const string PlacementOfficer = "PlacementOfficer";

        // All roles as array for seeding.
        public static readonly string[] AllRoles = new[]
        {
            SuperAdmin,
            Principal,
            HOD,
            Faculty,
            ContentEditor,
            PlacementOfficer
        };
    }
}