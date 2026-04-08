using Microsoft.AspNetCore.Identity;
using NuGet.Protocol.Plugins;

namespace GECPatan.Admin.Models.Domain
{
    public class ApplicationUser :IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        // Department the user belongs to.
        // Null for SuperAdmin, Principal, ContentEditor.
        public int? DeptId { get; set; }
        public Department? Department { get; set; }

        // Linked faculty record.
        // Only set when user role is Faculty or HOD
        public int? FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        // Is this account active?
        public bool IsActive { get; set; } = true;

        // Profile photo path (optional).
        public string? ProfileImagePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
    }
}
