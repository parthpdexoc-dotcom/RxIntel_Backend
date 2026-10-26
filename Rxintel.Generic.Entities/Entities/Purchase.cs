using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Purchase")]
    public class Purchase : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();   

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        public Guid WholesalerId { get; set; }

        [Required]
        [MaxLength(20)]
        public string NDC { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityPurchased { get; set; }

        /// <summary>Total units ordered (QuantityPurchased × PackageSize). Calculated server-side.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalQuantityOrdered { get; set; }

        public string? PackageSize { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public Guid? ImportErrorFileId { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(ImportErrorFileId))]
        public ImportErrorFile? ImportErrorFile { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy? Pharmacy { get; set; }

        [ForeignKey("WholesalerId")]
        public Wholesaler? Wholesaler { get; set; }
    }
}