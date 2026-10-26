using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

public readonly record struct CsvFieldMapping(string SourceFieldName, string DestinationFieldName);

public sealed class CsvParsedRow<TDto>
{
    public int RowNumber { get; init; }
    public required TDto Dto { get; init; }
    public List<ImportErrorItemDto> ParseErrors { get; init; } = new();
}

/// <summary>
/// Maps CSV/Excel rows to DTOs using saved field mappings (billing, purchase, etc.).
/// </summary>
public static class CsvImportHelper
{
    private static readonly string[] DateFormats =
    {
        "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
        "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd", "dd-MM-yy", "dd/MM/yy"
    };

    public static List<TDto> Parse<TDto>(
        Stream csvStream,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new() =>
        ParseWithRowNumbers(csvStream, mappings, initializeDto).Rows.Select(x => x.Dto).ToList();

    public static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseWithRowNumbers<TDto>(
        Stream stream,
        string? fileName,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        if (ImportFileHelper.IsExcelFile(fileName))
            return ParseExcelWithRowNumbers(stream, fileName, mappings, initializeDto);

        return ParseCsvWithRowNumbers(stream, mappings, initializeDto);
    }

    public static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseWithRowNumbers<TDto>(
        Stream csvStream,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new() =>
        ParseCsvWithRowNumbers(csvStream, mappings, initializeDto);

    private static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseCsvWithRowNumbers<TDto>(
        Stream csvStream,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        using var reader = new StreamReader(csvStream);

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            return (new List<CsvParsedRow<TDto>>(), null);

        var headers = FieldMappingCsvHelper.ParseCsvHeader(headerLine);
        var columnMappingErrors = GetColumnMappingErrors(headers, mappings);
        if (columnMappingErrors.Count > 0)
            return (new List<CsvParsedRow<TDto>>(), columnMappingErrors);

        var dataRows = new List<(int RowNumber, IReadOnlyList<string> Cells)>();
        var rowNumber = 1;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            dataRows.Add((rowNumber, FieldMappingCsvHelper.ParseCsvHeader(line)));
        }

        return (ParseRowsWithRowNumbers(headers, dataRows, mappings, initializeDto), null);
    }

    private static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseExcelWithRowNumbers<TDto>(
        Stream stream,
        string? fileName,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        if (ImportFileHelper.IsLegacyExcelFile(fileName))
            return ParseLegacyExcelWithRowNumbers(stream, mappings, initializeDto);

        return ParseOpenXmlExcelWithRowNumbers(stream, mappings, initializeDto);
    }

    private static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseOpenXmlExcelWithRowNumbers<TDto>(
        Stream stream,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            return (new List<CsvParsedRow<TDto>>(), null);

        var firstRow = usedRange.FirstRow().RowNumber();
        var lastRow = usedRange.LastRow().RowNumber();
        var firstCol = usedRange.FirstColumn().ColumnNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();

        var headers = new List<string>();
        for (var col = firstCol; col <= lastCol; col++)
            headers.Add(GetOpenXmlCellString(worksheet.Cell(firstRow, col)));

        var columnMappingErrors = GetColumnMappingErrors(headers, mappings);
        if (columnMappingErrors.Count > 0)
            return (new List<CsvParsedRow<TDto>>(), columnMappingErrors);

        var dataRows = new List<(int RowNumber, IReadOnlyList<string> Cells)>();
        for (var row = firstRow + 1; row <= lastRow; row++)
        {
            var cells = new List<string>();
            var hasData = false;

            for (var col = firstCol; col <= lastCol; col++)
            {
                var value = GetOpenXmlCellString(worksheet.Cell(row, col));
                if (!string.IsNullOrWhiteSpace(value))
                    hasData = true;

                cells.Add(value);
            }

            if (!hasData)
                continue;

            dataRows.Add((row, cells));
        }

        return (ParseRowsWithRowNumbers(headers, dataRows, mappings, initializeDto), null);
    }

    private static (List<CsvParsedRow<TDto>> Rows, List<ImportErrorItemDto>? ColumnMappingErrors) ParseLegacyExcelWithRowNumbers<TDto>(
        Stream stream,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        using var workbook = new HSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null || sheet.PhysicalNumberOfRows == 0)
            return (new List<CsvParsedRow<TDto>>(), null);

        var headerRowIndex = sheet.FirstRowNum;
        var headerRow = sheet.GetRow(headerRowIndex);
        if (headerRow == null)
            return (new List<CsvParsedRow<TDto>>(), null);

        var firstCol = headerRow.FirstCellNum;
        var lastCol = headerRow.LastCellNum - 1;
        if (firstCol < 0 || lastCol < firstCol)
            return (new List<CsvParsedRow<TDto>>(), null);

        var headers = new List<string>();
        for (var col = firstCol; col <= lastCol; col++)
            headers.Add(GetLegacyExcelCellString(headerRow.GetCell(col)));

        var columnMappingErrors = GetColumnMappingErrors(headers, mappings);
        if (columnMappingErrors.Count > 0)
            return (new List<CsvParsedRow<TDto>>(), columnMappingErrors);

        var dataRows = new List<(int RowNumber, IReadOnlyList<string> Cells)>();
        for (var rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null)
                continue;

            var cells = new List<string>();
            var hasData = false;

            for (var col = firstCol; col <= lastCol; col++)
            {
                var value = GetLegacyExcelCellString(row.GetCell(col));
                if (!string.IsNullOrWhiteSpace(value))
                    hasData = true;

                cells.Add(value);
            }

            if (!hasData)
                continue;

            dataRows.Add((rowIndex + 1, cells));
        }

        return (ParseRowsWithRowNumbers(headers, dataRows, mappings, initializeDto), null);
    }

    private static List<ImportErrorItemDto> GetColumnMappingErrors(
        IReadOnlyList<string> headers,
        IEnumerable<CsvFieldMapping> mappings)
    {
        var normalizedHeaders = new HashSet<string>(
            headers.Select(FieldMappingCsvHelper.NormalizeFieldName).Where(h => !string.IsNullOrEmpty(h)),
            StringComparer.OrdinalIgnoreCase);

        var missing = mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.SourceFieldName))
            .Select(m => m.SourceFieldName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(source => !normalizedHeaders.Contains(FieldMappingCsvHelper.NormalizeFieldName(source)))
            .ToList();

        return missing.Count == 0
            ? new List<ImportErrorItemDto>()
            : ImportErrorHelper.ColumnMappingFileErrors(missing);
    }

    private static List<CsvParsedRow<TDto>> ParseRowsWithRowNumbers<TDto>(
        IReadOnlyList<string> headers,
        IEnumerable<(int RowNumber, IReadOnlyList<string> Cells)> dataRows,
        IEnumerable<CsvFieldMapping> mappings,
        Action<TDto> initializeDto) where TDto : new()
    {
        var result = new List<CsvParsedRow<TDto>>();
        var headerIndex = BuildHeaderIndex(headers, mappings);

        foreach (var (rowNumber, cells) in dataRows)
        {
            var row = MapRow(cells, headerIndex, initializeDto, rowNumber);
            if (row != null)
                result.Add(row);
        }

        return result;
    }

    private static string GetOpenXmlCellString(IXLCell cell)
    {
        if (cell.IsEmpty())
            return string.Empty;

        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        if (cell.DataType == XLDataType.Number)
        {
            if (cell.Style.DateFormat != null && !string.IsNullOrWhiteSpace(cell.Style.DateFormat.Format))
            {
                try
                {
                    return cell.GetDateTime().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch
                {
                    // ignore and format as number
                }
            }

            var value = cell.GetDouble();
            return value == Math.Truncate(value)
                ? ((long)value).ToString(CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString().Trim();
    }

    private static string GetLegacyExcelCellString(ICell? cell)
    {
        if (cell == null)
            return string.Empty;

        return cell.CellType switch
        {
            CellType.Numeric when DateUtil.IsCellDateFormatted(cell) =>
                FormatLegacyExcelDate(cell),
            CellType.Numeric => FormatLegacyExcelNumber(cell.NumericCellValue),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
            CellType.Formula => cell.CachedFormulaResultType switch
            {
                CellType.Numeric when DateUtil.IsCellDateFormatted(cell) =>
                    FormatLegacyExcelDate(cell),
                CellType.Numeric => FormatLegacyExcelNumber(cell.NumericCellValue),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                _ => string.Empty
            },
            _ => string.Empty
        };
    }

    private static string FormatLegacyExcelNumber(double value) =>
        value == Math.Truncate(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);

    private static string FormatLegacyExcelDate(ICell cell) =>
        DateUtil.GetJavaDate(cell.NumericCellValue, false).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static Dictionary<string, int> BuildHeaderIndex(
        IReadOnlyList<string> headers,
        IEnumerable<CsvFieldMapping> mappings)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var normalized = headers
            .Select((name, i) => (Name: FieldMappingCsvHelper.NormalizeFieldName(name), Index: i))
            .ToList();

        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourceFieldName))
                continue;

            var source = FieldMappingCsvHelper.NormalizeFieldName(mapping.SourceFieldName);
            var match = normalized.FirstOrDefault(h => h.Name == source);
            if (!string.IsNullOrEmpty(match.Name))
                index[mapping.DestinationFieldName.Trim()] = match.Index;
        }

        return index;
    }

    private static CsvParsedRow<TDto>? MapRow<TDto>(
        IReadOnlyList<string> cells,
        Dictionary<string, int> headerIndex,
        Action<TDto> initializeDto,
        int rowNumber) where TDto : new()
    {
        var dto = new TDto();
        initializeDto(dto);

        var parseErrors = new List<ImportErrorItemDto>();
        var hasData = false;

        foreach (var (destination, colIndex) in headerIndex)
        {
            if (colIndex >= cells.Count)
                continue;

            var value = cells[colIndex]?.Trim();
            if (string.IsNullOrEmpty(value))
                continue;

            hasData = true;
            if (TrySetValue(dto, destination, value, out var propertyName, out var error))
                continue;

            if (!string.IsNullOrWhiteSpace(propertyName) && !string.IsNullOrWhiteSpace(error))
                parseErrors.Add(ImportErrorHelper.RowError(rowNumber, propertyName, error, value));
        }

        if (!hasData)
            return null;

        if (parseErrors.Count > 0)
        {
            var ndc = ImportErrorHelper.GetFieldValue(dto!, "NDC");
            var rxOrInvoice = ImportErrorHelper.GetFieldValue(dto!, "RXNO")
                ?? ImportErrorHelper.GetFieldValue(dto!, "InvoiceNumber");

            foreach (var error in parseErrors)
            {
                error.NDC = ndc;
                error.RxNoOrInvoice = rxOrInvoice;
            }
        }

        return new CsvParsedRow<TDto> { RowNumber = rowNumber, Dto = dto, ParseErrors = parseErrors };
    }

    private static bool TrySetValue<TDto>(
        TDto dto,
        string destination,
        string value,
        out string? propertyName,
        out string? error)
    {
        propertyName = null;
        error = null;

        var property = typeof(TDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.CanWrite &&
                FieldMappingCsvHelper.NormalizeFieldName(p.Name) ==
                FieldMappingCsvHelper.NormalizeFieldName(destination));

        if (property == null)
            return false;

        propertyName = property.Name;
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var label = ImportFieldLabels.Format(property.Name);

        object? converted;
        if (type == typeof(string))
        {
            converted = value;
        }
        else if (type == typeof(int))
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                error = $"{label} must be a valid whole number.";
                return false;
            }
            converted = n;
        }
        else if (type == typeof(decimal))
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
            {
                error = $"{label} must be a valid number.";
                return false;
            }
            converted = n;
        }
        else if (type == typeof(DateTime))
        {
            if (!TryParseCsvDate(value, out var date))
            {
                error = $"{label} must be a valid date.";
                return false;
            }
            converted = date;
        }
        else
        {
            try
            {
                converted = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch
            {
                error = $"{label} has an invalid value.";
                return false;
            }
        }

        property.SetValue(dto, converted);
        return true;
    }

    private static bool TryParseCsvDate(string value, out DateTime date)
    {
        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out date)
            || DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate)
            && oaDate >= 30000 && oaDate <= 100000)
        {
            try
            {
                date = DateTime.FromOADate(oaDate);
                return true;
            }
            catch
            {
                // ignore
            }
        }

        date = default;
        return false;
    }
}
