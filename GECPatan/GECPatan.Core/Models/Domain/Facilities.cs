using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GECPatan.Core.Models.Domain
{
    // =============================================
    // FACILITY (Dynamic - Admin creates like Dept)
    // =============================================
    public class Facility : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Tagline { get; set; }

        public string? About { get; set; }

        public string? TitleImagePath { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<FacilityBannerImage> BannerImages { get; set; }
            = new List<FacilityBannerImage>();
        public ICollection<FacilityMember> Members { get; set; }
            = new List<FacilityMember>();
        public ICollection<FacilityVision> Visions { get; set; }
            = new List<FacilityVision>();
        public ICollection<FacilityMission> Missions { get; set; }
            = new List<FacilityMission>();
    }

    public class FacilityBannerImage : BaseEntity
    {
        public int Id { get; set; }
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class FacilityMember : BaseEntity
    {
        public int Id { get; set; }
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class FacilityVision : BaseEntity
    {
        public int Id { get; set; }
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        public string VisionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }

    public class FacilityMission : BaseEntity
    {
        public int Id { get; set; }
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        public string MissionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }
}