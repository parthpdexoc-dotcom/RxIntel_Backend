using System.ComponentModel.DataAnnotations;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class PharmacySoftwareDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Software Name is required")]
        [MaxLength(200, ErrorMessage = "Software Name cannot exceed 200 characters")]
        public string SoftwareName { get; set; }
    }
}
