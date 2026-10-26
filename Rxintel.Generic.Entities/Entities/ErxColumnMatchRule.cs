using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxColumnMatchRule")]
    public class ErxColumnMatchRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RuleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldGroupName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string MmColumnName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ErxColumnName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MatchType { get; set; } = "Exact"; // Exact, CaseInsensitive, NumericTolerance, Fuzzy, Ignore

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ToleranceValue { get; set; }

        public int? InsuranceCompanyId { get; set; }

        [MaxLength(500)]
        public string? InsuranceCompanyIds { get; set; }

        [MaxLength(1000)]
        public string? InsuranceCompanyNames { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(InsuranceCompanyId))]
        public InsuranceCompany? InsuranceCompany { get; set; }

        public ICollection<ErxAuditDiscrepancy> Discrepancies { get; set; } = new List<ErxAuditDiscrepancy>();
    }
}
