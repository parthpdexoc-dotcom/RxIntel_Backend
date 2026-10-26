namespace Rxintel.Generic.Services.Interfaces;

/// <summary>
/// Resolves short numeric NDC values against the openFDA NDC directory.
/// </summary>
public interface IOpenFdaNdcResolver
{
    /// <summary>
    /// Returns the FDA package NDC in 11-digit 5-4-2 format, or null when no
    /// matching package is present in the current FDA directory.
    /// </summary>
    Task<string?> ResolveToNdc11Async(string? ndc, CancellationToken cancellationToken = default);
}
