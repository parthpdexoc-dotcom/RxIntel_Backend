using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("WholesalerFieldMapping")]
    public class WholesalerFieldMapping : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();

        [Required]
        public Guid WholesalerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string SourceColumnName { get; set; }

        [Required]
        [MaxLength(100)]
        public string DestinationColumnName { get; set; }

        [ForeignKey("WholesalerId")]
        public Wholesaler? Wholesaler { get; set; }
    }
}
