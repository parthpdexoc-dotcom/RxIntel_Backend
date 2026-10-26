using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("ErxSigMapping")]
    public class ErxSigMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The Latin medical or standard abbreviation (e.g., "PO", "BID", "QHS", "PRN", "UD / AAA").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Abbreviation { get; set; } = string.Empty;

        /// <summary>
        /// The simple English human-readable translation (e.g., "By mouth (orally)", "Twice a day", "Every night at bedtime").
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string SimpleEnglish { get; set; } = string.Empty;

        /// <summary>
        /// Normalized clinical token used in comparison engine (e.g., "by mouth", "twice daily", "at bedtime").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string NormalizedToken { get; set; } = string.Empty;

        /// <summary>
        /// Category of the abbreviation: "Administration Route", "Frequency & Timing", "Dosage Form", "System Quantity & Condition", or "Custom".
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Custom";

        /// <summary>
        /// Clinical notes or Latin etymology (e.g., "Per Os - Oral ingestion").
        /// </summary>
        [MaxLength(250)]
        public string? ClinicalContext { get; set; }

        /// <summary>
        /// Whether this mapping is active in the audit comparison engine.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }
    }
}
