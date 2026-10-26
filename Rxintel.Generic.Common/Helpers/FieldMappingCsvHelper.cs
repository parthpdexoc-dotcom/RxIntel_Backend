namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Shared CSV header parsing and field-name normalization for billing / wholesaler field mapping services.
/// </summary>
public static class FieldMappingCsvHelper
{
    public static List<string> ParseCsvHeader(string headerLine)
    {
        var columns = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var ch in headerLine)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                columns.Add(current.Trim());
                current = string.Empty;
            }
            else
            {
                current += ch;
            }
        }

        columns.Add(current.Trim());
        return columns;
    }

    public static string NormalizeFieldName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}
