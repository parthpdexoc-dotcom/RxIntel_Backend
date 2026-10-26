using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("NDCGroup")]
    public class NDCGroup : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ParentMedicineId { get; set; }

        [Required]
        public Guid ChildMedicineId { get; set; }

        [ForeignKey(nameof(ParentMedicineId))]
        public Medicine? ParentMedicine { get; set; }

        [ForeignKey(nameof(ChildMedicineId))]
        public Medicine? ChildMedicine { get; set; }       
    }
}