using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("PharmacyBillingSoftware")]
    public class PharmacyBillingSoftware
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string SoftwareName { get; set; }
    }
}
