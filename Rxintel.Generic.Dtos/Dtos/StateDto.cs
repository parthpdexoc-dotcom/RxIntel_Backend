using System.ComponentModel.DataAnnotations;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class StateDto
    {
        public Guid StateId { get; set; }

        [Required(ErrorMessage = "State Name is required")]
        [MaxLength(200, ErrorMessage = "State Name cannot exceed 200 characters")]
        public string StateName { get; set; }

        [MaxLength(2, ErrorMessage = "State Code cannot exceed 2 characters")]
        public string? StateCode { get; set; }

        [Required]
        [MaxLength(32)]
        public string StateBoardLicenseRuleId { get; set; } = "NUM_5_10";
    }
}
