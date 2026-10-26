using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Billing")]
    public class Billing : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();   

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        public Guid InsuranceId { get; set; }

        [Required]
        [MaxLength(20)]
        public string NDC { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityBilled { get; set; }

        [MaxLength(50)]
        public string? RXNO { get; set; }

        [Required]
        public DateTime BillDate { get; set; }
       
        [Required]
        [MaxLength(200)]
        public string DrugName { get; set; }

        [MaxLength(100)]
        public string? DrugForm { get; set; }

        [MaxLength(50)]
        public string? PackageSize { get; set; }

        [MaxLength(200)]
        public string? DrugTherapy { get; set; }

        public int? Days { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrimaryInsurancePaidAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PrimaryInsuranceBinNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SecondaryInsurancePaidAmount { get; set; }

        public Guid? SecondaryInsuranceId { get; set; }

        [MaxLength(100)]
        public string? SecondaryInsurance { get; set; }

        [MaxLength(50)]
        public string? SecondaryInsuranceBinNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalInsurancePaidAmount { get; set; }

        public Guid? ImportErrorFileId { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(ImportErrorFileId))]
        public ImportErrorFile? ImportErrorFile { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy? Pharmacy { get; set; }

        [ForeignKey("InsuranceId")]
        public Insurance? Insurance { get; set; }

        [ForeignKey("SecondaryInsuranceId")]
        public Insurance? SecondaryInsuranceProvider { get; set; }
    }
}