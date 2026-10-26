using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Pharmacy")]
    public class Pharmacy : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [MaxLength(50)]
        public string DEA { get; set; }

        public DateTime? DeaExpirationDate { get; set; }

        [MaxLength(200)]
        public string? BusinessLegalName { get; set; }

        [Required]
        [MaxLength(200)]
        public string BusinessTradeName { get; set; }

        [MaxLength(50)]
        public string? NPI { get; set; }

        [MaxLength(200)]
        public string? PharmacyAddress { get; set; }

        [MaxLength(200)]
        public string? StreetAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public Guid? StateId { get; set; }

        [ForeignKey("StateId")]
        public State? State { get; set; }

        public Guid? PharmacySoftwareId { get; set; }

        [ForeignKey("PharmacySoftwareId")]
        public PharmacyBillingSoftware? PharmacyBillingSoftware { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? Fax { get; set; }

        [MaxLength(50)]
        public string? StateBoardOfPharmacyLicenseNumber { get; set; }

        public DateTime? BopLicenseExpirationDate { get; set; }

        [MaxLength(13)]
        public string? GlobalLocationNumber { get; set; }

        [MaxLength(100)]
        public string? PharmacyBillingSoftwareName { get; set; }

        [MaxLength(20)]
        public string? PharmacyBillingSoftwarePhone { get; set; }

        [MaxLength(255)]
        public string? PharmacyBillingSoftwareEmail { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [Required]
        public bool IsActive { get; set; }       
    }
}