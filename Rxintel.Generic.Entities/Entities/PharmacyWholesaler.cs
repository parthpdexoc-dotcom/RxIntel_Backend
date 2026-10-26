using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("PharmacyWholesaler")]
    public class PharmacyWholesaler : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        public Guid WholesalerId { get; set; }

        [ForeignKey(nameof(PharmacyId))]
        public Pharmacy? Pharmacy { get; set; }

        [ForeignKey(nameof(WholesalerId))]
        public Wholesaler? Wholesaler { get; set; }
    }
}
