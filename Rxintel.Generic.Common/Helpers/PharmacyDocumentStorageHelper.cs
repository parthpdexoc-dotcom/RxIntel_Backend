using Microsoft.AspNetCore.Http;

namespace Rxintel.Generic.Common.Helpers;

public static class PharmacyDocumentStorageHelper
{
    public const string DeaDocumentName = "dea";
    public const string LicenseDocumentName = "license";
    public const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    public static bool HasFileContent(IFormFile? file) =>
        file != null && file.Length > 0;

    public static bool IsAllowedDocument(IFormFile? file)
    {
        if (!HasFileContent(file))
            return false;

        var extension = Path.GetExtension(file!.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public static bool IsWithinSizeLimit(IFormFile? file, long maxFileSizeBytes = DefaultMaxFileSizeBytes) =>
        !HasFileContent(file) || file!.Length <= maxFileSizeBytes;

    public static string GetPharmacyDocumentsDirectory(string contentRootPath, Guid pharmacyId) =>
        Path.Combine(contentRootPath, "Uploads", "PharmacyDocuments", pharmacyId.ToString());

    public static string? FindDocumentPath(string contentRootPath, Guid pharmacyId, string documentName)
    {
        var pharmacyFolder = GetPharmacyDocumentsDirectory(contentRootPath, pharmacyId);
        if (!Directory.Exists(pharmacyFolder))
            return null;

        var matches = Directory.GetFiles(pharmacyFolder, $"{documentName}.*");
        return matches.Length > 0 ? matches[0] : null;
    }

    public static bool DocumentExists(string contentRootPath, Guid pharmacyId, string documentName) =>
        FindDocumentPath(contentRootPath, pharmacyId, documentName) != null;

    public static string? GetDocumentFileName(string contentRootPath, Guid pharmacyId, string documentName)
    {
        var path = FindDocumentPath(contentRootPath, pharmacyId, documentName);
        return path != null ? Path.GetFileName(path) : null;
    }

    public static string GetContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

    public static async Task SaveDocument(
        string contentRootPath,
        Guid pharmacyId,
        string documentName,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!HasFileContent(file))
            return;

        var extension = Path.GetExtension(file.FileName);
        var pharmacyFolder = Path.Combine(
            contentRootPath,
            "Uploads",
            "PharmacyDocuments",
            pharmacyId.ToString());

        Directory.CreateDirectory(pharmacyFolder);

        foreach (var existingFile in Directory.GetFiles(pharmacyFolder, $"{documentName}.*"))
        {
            File.Delete(existingFile);
        }

        var destinationPath = Path.Combine(pharmacyFolder, $"{documentName}{extension}");
        await using var stream = File.Create(destinationPath);
        await file.CopyToAsync(stream, cancellationToken);
    }

    public static void DeletePharmacyDocumentsFolder(string contentRootPath, Guid pharmacyId)
    {
        var pharmacyFolder = Path.Combine(
            contentRootPath,
            "Uploads",
            "PharmacyDocuments",
            pharmacyId.ToString());

        if (Directory.Exists(pharmacyFolder))
            Directory.Delete(pharmacyFolder, recursive: true);
    }
}
