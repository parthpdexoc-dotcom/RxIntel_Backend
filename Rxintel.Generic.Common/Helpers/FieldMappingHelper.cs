using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using NPOI.HSSF.UserModel;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Shared logic for billing and wholesaler field-mapping services.
/// </summary>
public static class FieldMappingHelper
{
    public static async Task<List<string>> ReadColumnsAsync(Stream stream, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("Stream is invalid", nameof(stream));

        if (ImportFileHelper.IsExcelFile(fileName))
        {
            if (ImportFileHelper.IsLegacyExcelFile(fileName))
            {
                return ReadLegacyExcelColumns(stream);
            }

            return ReadOpenXmlExcelColumns(stream);
        }

        return await ReadCsvColumnsAsync(stream);
    }

    public static async Task<List<string>> ReadCsvColumnsAsync(Stream csvStream)
    {
        ArgumentNullException.ThrowIfNull(csvStream);
        if (!csvStream.CanRead)
            throw new ArgumentException("CSV stream is invalid", nameof(csvStream));

        using var reader = new StreamReader(csvStream);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return [];

        return FieldMappingCsvHelper.ParseCsvHeader(headerLine)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ReadOpenXmlExcelColumns(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return [];

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            return [];

        var firstRow = usedRange.FirstRow().RowNumber();
        var firstCol = usedRange.FirstColumn().ColumnNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();

        var headers = new List<string>();
        for (var col = firstCol; col <= lastCol; col++)
        {
            var text = worksheet.Cell(firstRow, col).GetFormattedString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
                headers.Add(text);
        }

        return headers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> ReadLegacyExcelColumns(Stream stream)
    {
        using var workbook = new HSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null || sheet.PhysicalNumberOfRows == 0)
            return [];

        var headerRow = sheet.GetRow(sheet.FirstRowNum);
        if (headerRow == null)
            return [];

        var firstCol = headerRow.FirstCellNum;
        var lastCol = headerRow.LastCellNum - 1;
        if (firstCol < 0 || lastCol < firstCol)
            return [];

        var headers = new List<string>();
        for (var col = firstCol; col <= lastCol; col++)
        {
            var cell = headerRow.GetCell(col);
            var text = cell?.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                headers.Add(text);
        }

        return headers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string? FindMatchingCsvColumn(IReadOnlyList<string>? csvHeaders, string destinationColumn) =>
        csvHeaders?.FirstOrDefault(h =>
            FieldMappingCsvHelper.NormalizeFieldName(h)
            == FieldMappingCsvHelper.NormalizeFieldName(destinationColumn));

    public static List<TRow> BuildRows<TRow>(
        IReadOnlyList<string> destinationColumns,
        IReadOnlyList<string>? csvHeaders,
        Func<string?, string, TRow> createRow) =>
        destinationColumns
            .Select(dest => createRow(FindMatchingCsvColumn(csvHeaders, dest), dest))
            .ToList();

    public static async Task<(TPreview? Preview, string? Error)> GetPreviewFromUploadAsync<TPreview>(
        IFormFile? file,
        Func<Stream, string?, Task<TPreview>> buildPreviewAsync,
        Action<TPreview> setSuccessMessage) where TPreview : class
    {
        if (file == null || file.Length == 0)
            return (null, CustomMessage.FieldMappingCsvFileRequired);

        var uploadedFileName = ImportFileHelper.GetUploadedFileName(file);
        if (!ImportFileHelper.IsSupportedImportFile(uploadedFileName))
            return (null, CustomMessage.BillingPurchaseCsvFileRequired);

        await using var stream = file.OpenReadStream();
        var preview = await buildPreviewAsync(stream, uploadedFileName);
        setSuccessMessage(preview);
        return (preview, null);
    }

    public static async Task DeleteExistingBeforeBulkUpsertAsync<TDto>(
        IReadOnlyList<TDto> items,
        Func<TDto, Guid> getParentId,
        Func<Guid, Task> deleteByParentIdAsync)
    {
        if (items.Count > 0)
            await deleteByParentIdAsync(getParentId(items[0]));
    }
}
