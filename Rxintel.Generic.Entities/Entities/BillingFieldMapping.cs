using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("BillingFieldMapping")]
    public class BillingFieldMapping : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();   

        [Required]
        public Guid PharmacySoftwareId { get; set; }

        [MaxLength(200)]
        public string? SourceFieldName { get; set; }

        [Required]
        [MaxLength(100)]
        public string DestinationFieldName { get; set; }
       
        [ForeignKey("PharmacySoftwareId")]
        public PharmacyBillingSoftware? PharmacyBillingSoftware { get; set; }
    }
}