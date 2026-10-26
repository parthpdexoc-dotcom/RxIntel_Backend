using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Rxintel.Generic.Entities.Entities
{
    [Table("Admin")]
    public class Admin : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }  = Guid.NewGuid();

        [MaxLength(50)]
        public string? Password { get; set; }

        [MaxLength(50)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; }

        public string? ProfileImage { get; set; }
    }
}