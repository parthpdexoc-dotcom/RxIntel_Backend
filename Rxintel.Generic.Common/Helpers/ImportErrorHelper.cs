using System.Text;
using System.Reflection;
using System.Text.Json;
using ClosedXML.Excel;
using NPOI.HSSF.UserModel;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

public static class ImportErrorHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static ImportErrorItemDto ConfigurationFileError(string message) =>
        FileError(ImportErrorCategory.ConfigurationError, message);

    public static ImportErrorItemDto FieldMappingFileError(string message) =>
        FileError(ImportErrorCategory.FieldMappingError, message);

    public static ImportErrorItemDto ColumnMappingFileError(string message) =>
        FileError(ImportErrorCategory.ColumnMappingError, message);

    public static ImportErrorItemDto ValidationFileError(string message) =>
        FileError(ImportErrorCategory.ValidationError, message);

    public static ImportErrorItemDto RowError(
        int row,
        string fieldName,
        string message,
        string? fieldValue = null,
        string? ndc = null,
        string? rxNoOrInvoice = null) =>
        new()
        {
            Row = row,
            ErrorType = ImportErrorCategory.ValidationError,
            FieldName = fieldName,
            FieldValue = fieldValue,
            NDC = ndc,
            RxNoOrInvoice = rxNoOrInvoice,
            ErrorMessage = message
        };

    public static string? GetFieldValue(object row, string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || propertyName == "Row")
            return null;

        var property = row.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        return property?.GetValue(row)?.ToString();
    }

    public static List<ImportErrorItemDto> SingleConfigurationFileError(string message) =>
        new() { ConfigurationFileError(message) };

    public static List<ImportErrorItemDto> SingleFieldMappingFileError(string message) =>
        new() { FieldMappingFileError(message) };

    public static List<ImportErrorItemDto> SingleValidationFileError(string message) =>
        new() { ValidationFileError(message) };

    public static List<ImportErrorItemDto> ColumnMappingFileErrors(IEnumerable<string> missingSourceColumns) =>
        missingSourceColumns
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => ColumnMappingFileError(CustomMessage.RequiredMappedColumnNotFound(c.Trim())))
            .ToList();

    public static string FormatMessages(IReadOnlyList<ImportErrorItemDto> errors) =>
        string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage));

    public static List<ImportErrorItemDto> Dedupe(IEnumerable<ImportErrorItemDto> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ImportErrorItemDto>();

        foreach (var error in errors)
        {
            var key = string.Join(
                '|',
                error.Row,
                NormalizeForDedupe(error.FieldName),
                error.FieldValue?.Trim(),
                NormalizeForDedupe(error.ErrorMessage));

            if (seen.Add(key))
                result.Add(error);
        }

        return result;
    }

    /// <summary>
    /// Ignores casing and trailing punctuation so the same problem worded slightly differently by
    /// two code paths (e.g. "... 6 digits." and "... 6 digits") is reported once.
    /// </summary>
    private static string NormalizeForDedupe(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().TrimEnd('.', '!').ToLowerInvariant();

    /// <summary>
    /// Adds business/validation errors except for fields that already failed CSV type parsing.
    /// </summary>
    public static void AppendWithoutParseDuplicates(
        List<ImportErrorItemDto> target,
        IReadOnlyList<ImportErrorItemDto> parseErrors,
        IEnumerable<ImportErrorItemDto> validationErrors)
    {
        target.AddRange(parseErrors);

        var failedFields = new HashSet<string>(
            parseErrors.Where(e => !string.IsNullOrWhiteSpace(e.FieldName)).Select(e => e.FieldName!),
            StringComparer.OrdinalIgnoreCase);

        target.AddRange(validationErrors.Where(e =>
            string.IsNullOrWhiteSpace(e.FieldName) || !failedFields.Contains(e.FieldName)));
    }

    /// <summary>
    /// Maximum number of row errors echoed back in an upload response. The full set is always
    /// persisted to the import error report, which the caller can page through or download.
    /// </summary>
    public const int MaxResponseErrors = 500;

    /// <summary>
    /// Flattens every failing field of every failing row for an API response, so one upload
    /// tells the user about all bad columns instead of one at a time.
    /// </summary>
    public static List<ErrorDetail> ToErrorDetails(IReadOnlyList<ImportErrorItemDto> errors) =>
        errors
            .OrderBy(e => e.Row)
            .ThenBy(e => e.FieldName, StringComparer.OrdinalIgnoreCase)
            .Take(MaxResponseErrors)
            .Select(e => new ErrorDetail(
                e.Row > 0 ? $"Row {e.Row}: {e.ErrorMessage}" : e.ErrorMessage,
                ErrorType.Error,
                string.Empty,
                ResolveDetailErrorType(new[] { e })))
            .ToList();

    public static List<ImportErrorDetail> GroupIntoDetails(IReadOnlyList<ImportErrorItemDto> errors)
    {
        return errors
            .GroupBy(e => e.Row)
            .Select(group =>
            {
                var items = group.ToList();
                var first = items[0];
                var detailErrorType = ResolveDetailErrorType(items);
                var payload = new ImportErrorDetailsJsonDto
                {
                    Row = group.Key,
                    Data = items.Select(e => new ImportErrorFieldDto
                    {
                        FieldName = e.FieldName ?? string.Empty,
                        FieldValue = e.FieldValue ?? string.Empty,
                        ErrorMessage = e.ErrorMessage
                    }).ToList()
                };

                return new ImportErrorDetail
                {
                    ErrorType = detailErrorType,
                    NDC = first.NDC,
                    RxNoOrInvoice = first.RxNoOrInvoice,
                    ErrorMessagesJson = JsonSerializer.Serialize(payload, JsonOptions)
                };
            })
            .ToList();
    }

    /// <summary>Row number stored alongside the field errors; 0 for reports saved before it was tracked.</summary>
    public static int ParseRow(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return 0;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("row", out var rowElement)
                && rowElement.TryGetInt32(out var row))
            {
                return row;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    public static List<ImportErrorFieldDto> ParseData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<ImportErrorFieldDto>();

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<ImportErrorFieldDto>>(dataElement.GetRawText(), JsonOptions)
                           ?? new List<ImportErrorFieldDto>();
                }

                if (dataElement.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("errorMessages", out var legacyMessages))
                {
                    return ParseLegacyErrorMessages(legacyMessages, dataElement);
                }
            }

            if (root.ValueKind == JsonValueKind.Array)
                return ParseLegacyMessageArray(root);
        }
        catch
        {
            return new List<ImportErrorFieldDto>();
        }

        return new List<ImportErrorFieldDto>();
    }

    private static readonly string[] ExportHeaders =
        ["Row", "NDC", "Rx / Invoice No", "Field Name", "Field Value", "Error Message"];

    public static byte[] ToCsv(IEnumerable<ImportErrorExportRowDto> errors)
    {
        using var writer = new StringWriter();
        writer.WriteLine(string.Join(',', ExportHeaders));

        foreach (var error in errors)
        {
            writer.WriteLine(string.Join(',', ExportCells(error).Select(EscapeCsv)));
        }

        return Encoding.UTF8.GetBytes(writer.ToString());
    }

    public static byte[] ToExcel(IEnumerable<ImportErrorExportRowDto> errors, string? sourceFileName = null)
    {
        if (ImportFileHelper.IsLegacyExcelFile(sourceFileName))
            return ToLegacyExcel(errors);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Errors");

        for (var col = 0; col < ExportHeaders.Length; col++)
            worksheet.Cell(1, col + 1).Value = ExportHeaders[col];

        var row = 2;
        foreach (var error in errors)
        {
            var cells = ExportCells(error);
            for (var col = 0; col < cells.Length; col++)
                worksheet.Cell(row, col + 1).Value = cells[col];

            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] ToLegacyExcel(IEnumerable<ImportErrorExportRowDto> errors)
    {
        var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Errors");

        var headerRow = sheet.CreateRow(0);
        for (var col = 0; col < ExportHeaders.Length; col++)
            headerRow.CreateCell(col).SetCellValue(ExportHeaders[col]);

        var rowIndex = 1;
        foreach (var error in errors)
        {
            var row = sheet.CreateRow(rowIndex++);
            var cells = ExportCells(error);
            for (var col = 0; col < cells.Length; col++)
                row.CreateCell(col).SetCellValue(cells[col]);
        }

        using var stream = new MemoryStream();
        workbook.Write(stream, leaveOpen: true);
        return stream.ToArray();
    }

    private static string[] ExportCells(ImportErrorExportRowDto error) =>
    [
        error.Row > 0 ? error.Row.ToString() : string.Empty,
        error.NDC ?? string.Empty,
        error.RxNoOrInvoice ?? string.Empty,
        FormatExportFieldName(error.FieldName),
        error.FieldValue ?? string.Empty,
        error.ErrorMessage ?? string.Empty
    ];

    private static string FormatExportFieldName(string? fieldName) =>
        string.IsNullOrWhiteSpace(fieldName)
            ? string.Empty
            : ImportFieldLabels.Format(fieldName);

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    private static List<ImportErrorFieldDto> ParseLegacyMessageArray(JsonElement array)
    {
        var result = new List<ImportErrorFieldDto>();

        foreach (var item in array.EnumerateArray())
        {
            result.Add(new ImportErrorFieldDto
            {
                FieldName = item.TryGetProperty("errorType", out var errorType)
                    ? errorType.GetString() ?? string.Empty
                    : string.Empty,
                FieldValue = string.Empty,
                ErrorMessage = item.TryGetProperty("errorMessage", out var errorMessage)
                    ? errorMessage.GetString() ?? string.Empty
                    : string.Empty
            });
        }

        return result;
    }

    private static List<ImportErrorFieldDto> ParseLegacyErrorMessages(
        JsonElement messages,
        JsonElement rowData)
    {
        var fieldValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in rowData.EnumerateObject())
            fieldValues[property.Name] = property.Value.ToString();

        var result = new List<ImportErrorFieldDto>();

        foreach (var item in messages.EnumerateArray())
        {
            var fieldName = item.TryGetProperty("errorType", out var errorType)
                ? errorType.GetString() ?? string.Empty
                : string.Empty;

            fieldValues.TryGetValue(fieldName, out var fieldValue);

            result.Add(new ImportErrorFieldDto
            {
                FieldName = fieldName,
                FieldValue = fieldValue ?? string.Empty,
                ErrorMessage = item.TryGetProperty("errorMessage", out var errorMessage)
                    ? errorMessage.GetString() ?? string.Empty
                    : string.Empty
            });
        }

        return result;
    }

    private static ImportErrorItemDto FileError(string category, string message) =>
        new()
        {
            Row = 0,
            ErrorType = category,
            ErrorMessage = message
        };

    private static bool IsValidationItem(ImportErrorItemDto item) =>
        string.Equals(item.ErrorType, ImportErrorCategory.ValidationError, StringComparison.Ordinal);

    private static string ResolveDetailErrorType(IReadOnlyList<ImportErrorItemDto> items)
    {
        if (items.Count == 0)
            return ImportErrorCategory.ValidationError;

        if (items.Count == 1)
        {
            var item = items[0];
            if (item.Row == 0 || !IsValidationItem(item))
                return item.ErrorType;

            return ImportFieldLabels.Format(item.FieldName);
        }

        if (items.All(IsValidationItem))
            return ImportErrorCategory.ValidationError;

        var categories = items.Select(i => i.ErrorType).Distinct(StringComparer.Ordinal).ToList();
        return categories.Count == 1 ? categories[0] : ImportErrorCategory.ValidationError;
    }
}
