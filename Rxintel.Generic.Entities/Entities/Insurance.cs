using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Insurance")]
    public class Insurance:BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();
        
        [Required]
        [MaxLength(200)]
        public string InsuranceName { get; set; }

        [MaxLength(50)]
        public string? BINNumber { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }
    }
}