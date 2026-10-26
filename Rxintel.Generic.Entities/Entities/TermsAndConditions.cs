using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("TermsAndConditions")]
    public class TermsAndConditions : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Content { get; set; } = string.Empty;

        public Guid? UpdatedByAdminId { get; set; }

        [ForeignKey(nameof(UpdatedByAdminId))]
        public Admin? UpdatedByAdmin { get; set; }
    }
}
