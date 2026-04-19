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
        public const string GrievanceCoordinator = "GrievanceCoordinator";
        public const string CommitteeHead = "CommitteeHead";

        // All roles as array for seeding.
        public static readonly string[] AllRoles = new[]
        {
            SuperAdmin,
            Principal,
            HOD,
            Faculty,
            ContentEditor,
            PlacementOfficer,
            GrievanceCoordinator,
            CommitteeHead

        };
        // Teaching designations (IsTeaching = true)
        public static readonly string[] TeachingDesignations = new[]
        {
            "Professor",
            "Associate Professor",
            "Assistant Professor"
        };

        // Non-teaching designations (IsTeaching = false)
        public static readonly string[] NonTeachingDesignations = new[]
        {
            "Lab Assistant",
            "Technical Assistant",
            "Senior Technical Assistant",
            "Clerk",
            "Other"
        };

        // All designations combined
        public static readonly string[] AllDesignations =
            TeachingDesignations.Concat(NonTeachingDesignations).ToArray();

        // Qualification options for dropdown
        public static readonly string[] QualificationOptions = new[]
        {
            "PhD",
            "Pursuing PhD",
            "ME / MTech",
            "BE / BTech",
            "MCA",
            "MSc",
            "BSc",
            "Diploma",
            "Other"
        };
    }
}