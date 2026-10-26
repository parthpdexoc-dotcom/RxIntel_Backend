using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("PharmacyUser")]
    public class PharmacyUser : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }= Guid.NewGuid();   

        [Required]
        public Guid PharmacyId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [Required]      
        [MaxLength(150)]
        public string UserEmail { get; set; }

        [Required]
        [MaxLength(256)]
        public string Password { get; set; }

        public PharmacyUserRole? Role { get; set; }

        [MaxLength(150)]
        public string? PrimaryContactName { get; set; }

        [MaxLength(100)]
        public string? PrimaryContactTitle { get; set; }

        [MaxLength(20)]
        public string? PrimaryContactPhone { get; set; }

        [MaxLength(200)]
        public string? PrimaryWholesalerName { get; set; }

        [MaxLength(150)]
        public string? PrimaryWholesalerContactName { get; set; }

        [MaxLength(20)]
        public string? PrimaryWholesalerPhone { get; set; }

        [MaxLength(150)]
        public string? PrimaryWholesalerEmail { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public string? ProfileImage { get; set; }
        
        [ForeignKey("PharmacyId")]
        public Pharmacy? Pharmacy { get; set; }
    }
}