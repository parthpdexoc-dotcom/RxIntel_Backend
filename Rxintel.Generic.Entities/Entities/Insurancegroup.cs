using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("InsuranceGroup")]
    public class InsuranceGroup : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BINNumber { get; set; }

        [Required]
        public Guid ChildInsuranceId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [ForeignKey(nameof(ChildInsuranceId))]
        public Insurance? ChildInsurance { get; set; }
    }
}
