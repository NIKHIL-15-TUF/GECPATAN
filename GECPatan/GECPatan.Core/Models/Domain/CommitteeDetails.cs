using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Core.Models.Domain
{
    // VISION
    public class CommitteeVision : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string VisionText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }
    }
    // MISSION
    public class CommitteeMission : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string MissionText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }
    }

    // OBJECTIVE
    public class CommitteeObjective : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string ObjectiveText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }
    }
    // SUB OBJECTIVE
    public class CommitteeSubObjective : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string SubObjectiveText { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }
    }
    // COMMITTEE MEMBER
    public class CommitteeMember : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Contact { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }
    }
    // (e.g. "Faculty Representatives", "Student Members")
    public class AdditionalMemberGroup : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string GroupTitle { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int CommitteeId { get; set; }

        [ValidateNever]
        public CampusCommittee? Committee { get; set; }

        [ValidateNever]
        public ICollection<AdditionalMemberDetail> Members { get; set; }
            = new List<AdditionalMemberDetail>();
    }
    // ADDITIONAL MEMBER DETAIL
    public class AdditionalMemberDetail : BaseEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Position { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // FK
        public int AdditionalMemberGroupId { get; set; }

        [ValidateNever]
        public AdditionalMemberGroup? AdditionalMemberGroup { get; set; }
    }
}