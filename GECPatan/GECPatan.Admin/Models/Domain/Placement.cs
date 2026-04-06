using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.Domain
{
    // PLACEMENT STATISTIC
    public class PlacementStatistic : BaseEntity
    {
        public int Id { get; set; }

        // Branch info
        public int BranchId { get; set; }

        [Required, MaxLength(200)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int Passout { get; set; }
        public int Placed { get; set; }
        public int AverageCTC { get; set; }
        public int HigherStudy { get; set; }
        public int Business { get; set; }
        public double PlacementPercentage { get; set; }

        public bool IsDisplay { get; set; } = true;
    }

    // PLACEMENT TEAM MEMBER
    public class PlacementTeamMember : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        public string? ImagePath { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }
    // STUDENT CLUB
    public class StudentClub : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? About { get; set; }
        public string? Icon { get; set; }
        public string? BlogLink { get; set; }

        // Link to page
        public string? ActionName { get; set; }
        public string? ControllerName { get; set; }
        public bool IsDynamic { get; set; } = false;
        public int? DynamicId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        // Navigation
        [ValidateNever]
        public ICollection<ClubImage> Images { get; set; }
            = new List<ClubImage>();

        [ValidateNever]
        public ICollection<ClubMember> Members { get; set; }
            = new List<ClubMember>();

        [ValidateNever]
        public ICollection<ClubObjective> Objectives { get; set; }
            = new List<ClubObjective>();
    }

    // CLUB IMAGE
    public class ClubImage : BaseEntity
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        // FK
        public int ClubId { get; set; }

        [ValidateNever]
        public StudentClub? Club { get; set; }
    }

    // CLUB MEMBER
    public class ClubMember : BaseEntity
    {
        public int Id { get; set; }

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

        // FK
        public int ClubId { get; set; }

        [ValidateNever]
        public StudentClub? Club { get; set; }
    }

    // CLUB OBJECTIVE
    public class ClubObjective : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string ObjectiveText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int ClubId { get; set; }

        [ValidateNever]
        public StudentClub? Club { get; set; }
    }
}