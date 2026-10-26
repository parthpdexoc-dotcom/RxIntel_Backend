using System.ComponentModel.DataAnnotations;
namespace Rxintel.Generic.Dtos.Dtos
{
    public class InsuranceGroupDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "BINNumber is required")]
        [MaxLength(50)]
        public string BINNumber { get; set; }

        [Required(ErrorMessage = "ChildInsuranceId is required")]
        public Guid ChildInsuranceId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
