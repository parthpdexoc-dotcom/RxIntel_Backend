using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxAuditBatch")]
    public class ErxAuditBatch : BaseEntity
    {
        [Key]
        public Guid BatchId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int TotalRows { get; set; }

        public int TotalDiscrepancies { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Completed"; // Processing, Completed, Failed

        public Guid? PharmacyId { get; set; }

        [ForeignKey(nameof(PharmacyId))]
        public virtual Pharmacy? Pharmacy { get; set; }

        public ICollection<ErxAuditRecord> Records { get; set; } = new List<ErxAuditRecord>();
    }
}
