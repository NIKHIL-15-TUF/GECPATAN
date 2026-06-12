using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    // PLACEMENT STATISTIC
    public class PlacementStatistic : BaseEntity
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string? Year { get; set; }

        public int TotalPlaced { get; set; }
        public int TotalStudents { get; set; }
        public string? HighestPackage { get; set; }
        public string? AveragePackage { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class PlacementTeamMember : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; }

        public string? ImagePath { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        //public string? Email { get; set; }
        //public string? Mobile { get; set; }
    }
    // STUDENT CLUB
    public class StudentClub : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? BlogLink { get; set; }
        public string? ActionName { get; set; }
        public string? ControllerName { get; set; }
        public bool IsDynamic { get; set; } = false;
        public int? DynamicId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation
        public ICollection<ClubImage> Images { get; set; } = new List<ClubImage>();
        public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
        public ICollection<ClubObjective> Objectives { get; set; } = new List<ClubObjective>();
    }

    // Club Images (carousel on frontend)
    public class ClubImage : BaseEntity
    {
        public int Id { get; set; }

        public int ClubId { get; set; }

        [ForeignKey("ClubId")]
        public StudentClub? Club { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class ClubMember : BaseEntity
    {
        public int Id { get; set; }

        public int ClubId { get; set; }

        [ForeignKey("ClubId")]
        public StudentClub? Club { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class ClubObjective : BaseEntity
    {
        public int Id { get; set; }

        public int ClubId { get; set; }

        [ForeignKey("ClubId")]
        public StudentClub? Club { get; set; }

        public string ObjectiveText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }
}
