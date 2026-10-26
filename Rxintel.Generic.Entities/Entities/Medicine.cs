using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities
{
    [Table("Medicine")]
    public class Medicine : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string NDC { get; set; }

        /// <summary>NDC with hyphens removed. Persisted computed column for indexed lookups.</summary>
        [MaxLength(100)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? NormalizedNdc { get; private set; }

        [Required]
        [MaxLength(250)]
        public string Brand { get; set; }

        /// <summary>Strength value(s) (e.g. 10, 2; 8; 4.5; 7).</summary>
        [MaxLength(100)]
        [Column("Strength")]
        public string? Strength { get; set; }

        /// <summary>Strength unit labels (e.g. mg/mL, g/mL). Comma-separated when multiple.</summary>
        [MaxLength(500)]
        [Column("StrengthUnit")]
        public string? StrengthUnit { get; set; }

        [Required]
        [MaxLength(50)]
        public string PackageSize { get; set; }

        /// <summary>Dosage form labels (e.g. TABLET, SPRAY). Comma-separated when multiple.</summary>
        [MaxLength(250)]
        public string? DosageForm { get; set; }

        /// <summary>FDA product NDC (e.g. 41167-0337).</summary>
        [MaxLength(20)]
        public string? ProductNDC { get; set; }

        [MaxLength(500)]
        public string? Suffix { get; set; }

        [MaxLength(1000)]
        public string? Generic { get; set; }

        [MaxLength(150)]
        public string? Route { get; set; }

        [MaxLength(250)]
        public string? Labeler { get; set; }
    }
}

