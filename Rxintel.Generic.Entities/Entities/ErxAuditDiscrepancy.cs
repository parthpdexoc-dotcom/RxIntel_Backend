using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxAuditDiscrepancy")]
    public class ErxAuditDiscrepancy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DiscrepancyId { get; set; }

        [Required]
        public long RecordId { get; set; }

        [Required]
        public int RuleId { get; set; }

        [MaxLength(500)]
        public string? MmValue { get; set; }

        [MaxLength(500)]
        public string? ErxValue { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open, Reviewed, Ignored, Corrected

        [MaxLength(100)]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RecordId))]
        public ErxAuditRecord? Record { get; set; }

        [ForeignKey(nameof(RuleId))]
        public ErxColumnMatchRule? Rule { get; set; }
    }
}
