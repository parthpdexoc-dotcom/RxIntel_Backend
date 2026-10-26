using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxAuditAlertLog")]
    public class ErxAuditAlertLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AlertLogId { get; set; }

        [Required]
        public long RecordId { get; set; }

        [Required]
        public string AlertMessage { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string AlertType { get; set; } = "InApp";

        [ForeignKey(nameof(RecordId))]
        public ErxAuditRecord? Record { get; set; }
    }
}
