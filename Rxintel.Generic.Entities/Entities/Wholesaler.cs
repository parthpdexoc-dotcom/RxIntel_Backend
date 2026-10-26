using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Wholesaler")]
    public class Wholesaler : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();   

        [Required]
        [MaxLength(200)]
        public string WholesalerName { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        public bool Vawd { get; set; }

        public DateTime? VawdDate { get; set; }

        /// <summary>
        /// Backend-only. Recalculated whenever <see cref="VawdDate"/> is saved:
        /// the day before two years after that date (e.g. 01/09/2026 → 31/08/2028).
        /// Empty when <see cref="Vawd"/> is false or <see cref="VawdDate"/> is empty.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }
    }
}