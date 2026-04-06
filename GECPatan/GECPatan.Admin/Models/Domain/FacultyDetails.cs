using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    // PERSONAL DETAILS
    public class PersonalDetail : BaseEntity
    {
        public int PersonalDetailId { get; set; }

        public string? Department { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }

        // FK
        public int FacultyId { get; set; }

        [ValidateNever]
        public Faculty? Faculty { get; set; }
    }
    // EDUCATIONAL QUALIFICATIONS
    public class FacultyQualification : BaseEntity
    {
        public int FacultyQualificationId { get; set; }

        [Required, MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? University { get; set; }

        [MaxLength(20)]
        public string? Year { get; set; }

        public string? Specialization { get; set; }

        // FK
        public int FacultyId { get; set; }

        [ValidateNever]
        public Faculty? Faculty { get; set; }
    }
    // PROFESSIONAL EXPERIENCE
    public class FacultyExperience : BaseEntity
    {
        public int FacultyExperienceId { get; set; }

        [Required, MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Organization { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Duration { get; set; }

        // FK
        public int FacultyId { get; set; }

        [ValidateNever]
        public Faculty? Faculty { get; set; }
    }
    // TRAINING AND WORKSHOPS
    public class FacultyTraining : BaseEntity
    {
        public int FacultyTrainingId { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? OrganizedBy { get; set; }

        [MaxLength(100)]
        public string? Date { get; set; }

        // FK
        public int FacultyId { get; set; }

        [ValidateNever]
        public Faculty? Faculty { get; set; }
    }
    // PUBLICATIONS
    public class FacultyPublication : BaseEntity
    {
        public int FacultyPublicationId { get; set; }

        public int SrNo { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        // FK
        public int FacultyId { get; set; }

        [ValidateNever]
        public Faculty? Faculty { get; set; }
    }
}