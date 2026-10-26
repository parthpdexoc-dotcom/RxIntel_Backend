using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

public static class ImportFileHelper
{
    private static readonly HashSet<string> ExcelExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls", ".xlsm"
    };

    private static readonly Regex FileNameRegex = new(
        @"filename\*?=(?:UTF-8''(?<utf8>[^;]+)|""(?<quoted>[^""]+)"")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsExcelFile(string? fileName) =>
        ExcelExtensions.Contains(GetExtension(fileName));

    public static bool IsLegacyExcelFile(string? fileName) =>
        string.Equals(GetExtension(fileName), ".xls", StringComparison.OrdinalIgnoreCase);

    public static bool IsCsvFile(string? fileName) =>
        string.Equals(GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);

    public static bool IsSupportedImportFile(string? fileName) =>
        IsExcelFile(fileName) || IsCsvFile(fileName);

    public static string GetUploadedFileName(IFormFile? file)
    {
        var fileName = file?.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = ExtractFileNameFromContentDisposition(file?.ContentDisposition);

        return NormalizeFileName(fileName, string.Empty);
    }

    public static string NormalizeStoredFileName(string? fileName) =>
        NormalizeFileName(fileName, "upload.csv");

    public static string BuildErrorDownloadFileName(string? storedFileName)
    {
        var fileName = NormalizeFileName(storedFileName, string.Empty);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "import-errors";

        return $"{baseName}_errors{GetErrorDownloadExtension(fileName)}";
    }

    public static string GetErrorDownloadExtension(string? fileName)
    {
        var extension = GetExtension(fileName);
        return IsExcelFile(fileName) ? extension : ".csv";
    }

    public static string GetErrorDownloadContentType(string? fileName) =>
        GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xls" => "application/vnd.ms-excel",
            _ => "text/csv"
        };

    private static string NormalizeFileName(string? fileName, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return defaultFileName;

        fileName = fileName.Trim().Trim('"', '\'');
        fileName = fileName.Replace('\\', '/');
        var lastSlash = fileName.LastIndexOf('/');
        if (lastSlash >= 0)
            fileName = fileName[(lastSlash + 1)..];

        fileName = Path.GetFileName(fileName);

        return string.IsNullOrWhiteSpace(fileName) ? defaultFileName : fileName;
    }

    private static string GetExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        return Path.GetExtension(NormalizeFileName(fileName, string.Empty));
    }

    private static string? ExtractFileNameFromContentDisposition(string? contentDisposition)
    {
        if (string.IsNullOrWhiteSpace(contentDisposition))
            return null;

        var match = FileNameRegex.Match(contentDisposition);
        if (!match.Success)
            return null;

        if (match.Groups["utf8"].Success)
            return Uri.UnescapeDataString(match.Groups["utf8"].Value.Trim().Trim('"'));

        return match.Groups["quoted"].Value;
    }
}
