using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    public class ApplicationUser : IdentityUser  // ← IdentityUser not IdentityUser<string>
    {
        public string? FullName { get; set; }

        public int? DeptId { get; set; }
        public Department? Department { get; set; }   // ← navigation property needed by DbContext

        public int? FacultyId { get; set; }
        public Faculty? Faculty { get; set; }         // ← navigation property needed by DbContext

        public int? CommitteeId { get; set; }         
        public int? FacilityId { get; set; }          
        public int? ContentPageId{ get; set;}

        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = true;  // ← was missing

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(200)]
        public string? CreatedBy { get; set; }         // ← was missing
    }
}