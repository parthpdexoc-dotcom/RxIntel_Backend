using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("State")]
    public class State
    {
        [Key]
        public Guid StateId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string StateName { get; set; }

        [MaxLength(2)]
        public string? StateCode { get; set; }

        /// <summary>Rule key for state board license validation (e.g. NUM_5_10).</summary>
        [Required]
        [MaxLength(32)]
        public string StateBoardLicenseRuleId { get; set; } = "NUM_5_10";
    }
}
