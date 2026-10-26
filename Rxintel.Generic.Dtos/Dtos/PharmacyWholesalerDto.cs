using System.ComponentModel.DataAnnotations;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class PharmacyWholesalerDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "PharmacyId is required")]
        public Guid PharmacyId { get; set; }

        [Required(ErrorMessage = "WholesalerId is required")]
        public Guid WholesalerId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
