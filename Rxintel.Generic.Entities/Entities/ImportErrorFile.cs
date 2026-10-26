using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Entities.Entities;

[Table("ImportErrorFile")]
public class ImportErrorFile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public ImportType ImportType { get; set; }

    public Guid PharmacyId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public int ErrorCount { get; set; }

    /// <summary>Distinct CSV rows that failed validation (billing import summary).</summary>
    public int FailedRowCount { get; set; }

    /// <summary>Successfully imported billing rows linked to this file.</summary>
    public int TotalImportedRows { get; set; }

    [MaxLength(200)]
    public string? UploadedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool IsViewed { get; set; }

    public DateTime? ViewedDate { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy? Pharmacy { get; set; }

    public ICollection<ImportErrorDetail> Details { get; set; } = new List<ImportErrorDetail>();
}
