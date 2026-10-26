using System.ComponentModel.DataAnnotations;
namespace Rxintel.Generic.Dtos.Dtos
{
    public class NDCGroupDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "ParentMedicineId is required")]
        public Guid ParentMedicineId { get; set; }

        [Required(ErrorMessage = "ChildMedicineIds is required")]
        public List<Guid> ChildMedicineIds { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public List<Guid> OldChildMedicineIds { get; set; } = new();
    }
}
