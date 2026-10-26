using Microsoft.AspNetCore.Http;

namespace Rxintel.Generic.Common.Helpers;

public static class ProfileImageValidationHelper
{
    public const long MaxImageSizeBytes = 2 * 1024 * 1024;
    public const string ProfileImagesFolder = "Uploads/ProfileImages";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif"
    };

    public static bool IsValidProfileImageFilename(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var fileName = value.Trim();
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            return false;

        if (fileName.Contains("..", StringComparison.Ordinal))
            return false;

        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension)
               && AllowedExtensions.Contains(extension)
               && fileName.Length <= 255;
    }

    public static bool IsAllowedImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > MaxImageSizeBytes)
            return false;

        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public static string GetProfileImageDirectory(string contentRootPath) =>
        Path.Combine(contentRootPath, ProfileImagesFolder);

    public static string? GetProfileImagePath(string contentRootPath, string? fileName)
    {
        if (!IsValidProfileImageFilename(fileName))
            return null;

        var path = Path.Combine(GetProfileImageDirectory(contentRootPath), fileName!.Trim());
        return File.Exists(path) ? path : null;
    }

    public static string GetContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

    public static async Task<string> SaveProfileImageAsync(
        string contentRootPath,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);
        var safeName = new string(Path.GetFileNameWithoutExtension(file.FileName)
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "profile";

        var fileName = $"{Guid.NewGuid():N}_{safeName}{extension}".ToLowerInvariant();
        var directory = GetProfileImageDirectory(contentRootPath);
        Directory.CreateDirectory(directory);

        var destinationPath = Path.Combine(directory, fileName);
        await using var stream = File.Create(destinationPath);
        await file.CopyToAsync(stream, cancellationToken);

        return fileName;
    }

    public static void DeleteProfileImage(string contentRootPath, string? fileName)
    {
        if (!IsValidProfileImageFilename(fileName))
            return;

        var path = Path.Combine(GetProfileImageDirectory(contentRootPath), fileName!.Trim());
        if (File.Exists(path))
            File.Delete(path);
    }
}
