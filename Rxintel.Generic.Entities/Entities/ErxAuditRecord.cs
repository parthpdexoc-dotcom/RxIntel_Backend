using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxAuditRecord")]
    public class ErxAuditRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RecordId { get; set; }

        [Required]
        public Guid BatchId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RxNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MmFillNumber { get; set; }

        [MaxLength(50)]
        public string? ErxRxNumber { get; set; }

        public DateTime? MmOrderDate { get; set; }
        public DateTime? ErxOrderDate { get; set; }
        public DateTime? ErxEffectiveDate { get; set; }
        public DateTime? MmDateFilled { get; set; }
        public TimeSpan? MmTimeFilled { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? ErxReceivedDateTime { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(50)]
        public string? InsCode { get; set; }

        [MaxLength(50)]
        public string? BinNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalInsPaid { get; set; }

        [MaxLength(200)]
        public string? MmPatientName { get; set; }

        [MaxLength(200)]
        public string? ErxPatientName { get; set; }

        public DateTime? MmPatientDob { get; set; }
        public DateTime? ErxPatientDob { get; set; }

        [MaxLength(50)]
        public string? MmPatientPhone { get; set; }

        [MaxLength(50)]
        public string? ErxPatientPhone { get; set; }

        [MaxLength(300)]
        public string? MmPatientAddress { get; set; }

        [MaxLength(300)]
        public string? ErxPatientAddress { get; set; }

        [MaxLength(200)]
        public string? MmPrescriberName { get; set; }

        [MaxLength(200)]
        public string? ErxPrescriberName { get; set; }

        [MaxLength(200)]
        public string? SupervisorName { get; set; }

        [MaxLength(50)]
        public string? MmPrescriberNpi { get; set; }

        [MaxLength(50)]
        public string? ErxPrescriberNpi { get; set; }

        [MaxLength(50)]
        public string? MmPrescriberDea { get; set; }

        [MaxLength(50)]
        public string? ErxPrescriberDea { get; set; }

        [MaxLength(300)]
        public string? MmDispensedDrugName { get; set; }

        [MaxLength(300)]
        public string? ErxDrugName { get; set; }

        [MaxLength(100)]
        public string? MmPotencyUnitCode { get; set; }

        [MaxLength(100)]
        public string? ErxPotencyUnitCode { get; set; }

        [MaxLength(50)]
        public string? MmDispensedNdc { get; set; }

        [MaxLength(50)]
        public string? ErxNdc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MmQtyOrd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ErxQtyOrd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MmQtyDispensed { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ErxQtyDispensed { get; set; }

        public int? MmDispensedDaysSupply { get; set; }
        public int? ErxDaysSupply { get; set; }

        [MaxLength(500)]
        public string? MmDispensedSig { get; set; }

        [MaxLength(500)]
        public string? ErxSig { get; set; }

        public string? TelephoneNotes { get; set; }
        public DateTime? TelephoneNotesDate { get; set; }

        public string? ErxNotes { get; set; }
        public DateTime? ErxNotesDate { get; set; }

        [MaxLength(100)]
        public string? ErxNotesCreatedBy { get; set; }

        [MaxLength(50)]
        public string? MmDawCode { get; set; }

        [MaxLength(50)]
        public string? ErxDawCode { get; set; }

        public int? MmRefillsPharmacistEntered { get; set; }
        public int? ErxRefill { get; set; }
        public string? MmRphNotes { get; set; }
        public bool? MmSignatureCaptured { get; set; }

        [MaxLength(50)]
        public string? FollowUpTag { get; set; }

        [MaxLength(50)]
        public string? MmDelivery { get; set; }

        [MaxLength(50)]
        public string? MmRxOrigin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MmCopay { get; set; }

        [MaxLength(50)]
        public string? DurReasonCd { get; set; }

        [MaxLength(50)]
        public string? DurProfCd { get; set; }

        [MaxLength(50)]
        public string? DurResultCode { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(BatchId))]
        public ErxAuditBatch? Batch { get; set; }

        public ICollection<ErxAuditDiscrepancy> Discrepancies { get; set; } = new List<ErxAuditDiscrepancy>();
        public ICollection<ErxAuditAlertLog> AlertLogs { get; set; } = new List<ErxAuditAlertLog>();
    }
}
