using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rxintel.Generic.Entities.Entities;

[Table("ImportErrorDetail")]
public class ImportErrorDetail
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FileId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ErrorType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NDC { get; set; }

    [MaxLength(100)]
    public string? RxNoOrInvoice { get; set; }

    /// <summary>
    /// JSON array of { "ErrorType", "ErrorMessage" } for all errors on this CSV row.
    /// </summary>
    [Required]
    public string ErrorMessagesJson { get; set; } = "{\"data\":[]}";

    [ForeignKey(nameof(FileId))]
    public ImportErrorFile? File { get; set; }
}
