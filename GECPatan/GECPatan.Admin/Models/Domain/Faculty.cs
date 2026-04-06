using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    public class Faculty : BaseEntity
    {
        public int FacultyId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Designation { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        [Required]
        public DateTime DateOfJoining { get; set; }

        public string? Qualification { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? Website { get; set; }

        public bool IsTeaching { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Ordering on frontend
        public int SeniorityOrder { get; set; } = 0;

        // Department link
        public int DeptId { get; set; }

        [ValidateNever]
        public Department? Department { get; set; }

        // Navigation
        [ValidateNever]
        public PersonalDetail? PersonalDetail { get; set; }

        [ValidateNever]
        public ICollection<FacultyQualification> Qualifications { get; set; }
            = new List<FacultyQualification>();

        [ValidateNever]
        public ICollection<FacultyExperience> Experiences { get; set; }
            = new List<FacultyExperience>();

        [ValidateNever]
        public ICollection<FacultyTraining> Trainings { get; set; }
            = new List<FacultyTraining>();

        [ValidateNever]
        public ICollection<FacultyPublication> Publications { get; set; }
            = new List<FacultyPublication>();
    }
}