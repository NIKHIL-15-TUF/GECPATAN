namespace GECPatan.Admin.Models.Domain
{
    // Base class for all domain entities.
    // Provides audit fields and soft delete.
    public abstract class BaseEntity
    {
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public long CreatedDateInt { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public long? UpdatedDateInt { get; set; }

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}