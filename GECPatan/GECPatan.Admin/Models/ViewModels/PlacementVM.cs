using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── PLACEMENT STATISTIC ───────────────────────────────
    public class PlacementStatisticVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        [Display(Name = "Branch Name")]
        [MaxLength(200)]
        public string BranchName { get; set; } = string.Empty;

        public int BranchId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int Passout { get; set; }
        public int Placed { get; set; }
        public int AverageCTC { get; set; }
        public int HigherStudy { get; set; }
        public int Business { get; set; }

        [Display(Name = "Placement %")]
        public double PlacementPercentage { get; set; }

        public bool IsDisplay { get; set; } = true;
    }

    public class PlacementStatisticListVM
    {
        public int Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public int Passout { get; set; }
        public int Placed { get; set; }
        public double PlacementPercentage { get; set; }
        public bool IsDisplay { get; set; }
    }

    // ── PLACEMENT TEAM MEMBER ─────────────────────────────
    public class PlacementTeamMemberVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Designation { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public string? ExistingImagePath { get; set; }
    }
}