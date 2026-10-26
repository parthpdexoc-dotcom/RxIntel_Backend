using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Dtos.Dtos;

public class ImportErrorFieldDto
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ImportErrorItemDto
{
    public int Row { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? FieldValue { get; set; }
    public string? NDC { get; set; }
    public string? RxNoOrInvoice { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ImportErrorDetailsJsonDto
{
    public int Row { get; set; }
    public List<ImportErrorFieldDto> Data { get; set; } = new();
}

/// <summary>One flattened line in the downloadable error report.</summary>
public class ImportErrorExportRowDto
{
    public int Row { get; set; }
    public string? NDC { get; set; }
    public string? RxNoOrInvoice { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ImportErrorFileDto
{
    public Guid Id { get; set; }
    public ImportType ImportType { get; set; }
    public Guid PharmacyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string PharmacyName { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int FailedRowCount { get; set; }
    public int TotalImportedRows { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsViewed { get; set; }
    public DateTime? ViewedDate { get; set; }
}

public class ImportErrorDetailDto
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public int Row { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string? NDC { get; set; }
    public string? RxNoOrInvoice { get; set; }
    public List<ImportErrorFieldDto> Data { get; set; } = new();
}

public class ImportErrorUnreadCountDto
{
    public int BillingCount { get; set; }
    public int PurchaseCount { get; set; }
}

public class ImportErrorCsvDownloadDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
}
