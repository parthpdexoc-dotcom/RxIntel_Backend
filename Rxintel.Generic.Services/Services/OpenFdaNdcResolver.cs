using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public sealed class OpenFdaNdcResolver : IOpenFdaNdcResolver
{
    private readonly HttpClient _httpClient;
    private readonly IMedicineRepository _medicineRepository;
    private readonly ILogger<OpenFdaNdcResolver> _logger;
    private readonly Dictionary<string, string?> _cache = new(StringComparer.Ordinal);

    public OpenFdaNdcResolver(
        HttpClient httpClient,
        IMedicineRepository medicineRepository,
        ILogger<OpenFdaNdcResolver> logger)
    {
        _httpClient = httpClient;
        _medicineRepository = medicineRepository;
        _logger = logger;
    }

    public async Task<string?> ResolveToNdc11Async(
        string? ndc,
        CancellationToken cancellationToken = default)
    {
        var digits = ValidationHelperBase.GetPlainNdcDigits(ndc)
            ?? ValidationHelperBase.NormalizeNdc(ndc);
        if (string.IsNullOrWhiteSpace(digits)
            || !digits.All(char.IsAsciiDigit)
            || digits.Length is < 8 or > 10)
        {
            return null;
        }

        if (_cache.TryGetValue(digits, out var cached))
            return cached;

        var hyphenCandidates = BuildPackageNdcCandidates(digits);
        var ndc11Candidates = hyphenCandidates
            .Select(candidate => TryConvertToNdc11(candidate, out var ndc11) ? ndc11 : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var preferredNdc11 = digits.PadLeft(11, '0');
        var fromCatalog = await FindInMedicineCatalogAsync(ndc11Candidates, preferredNdc11);
        if (fromCatalog != null)
        {
            _cache[digits] = fromCatalog;
            return fromCatalog;
        }

        var fromFda = await FindPackageNdcAsync(hyphenCandidates, cancellationToken);
        _cache[digits] = fromFda;
        return fromFda;
    }

    private async Task<string?> FindInMedicineCatalogAsync(
        IReadOnlyList<string> ndc11Candidates,
        string preferredNdc11)
    {
        if (ndc11Candidates.Count == 0)
            return null;

        var matches = (await _medicineRepository.GetAllAsync(medicine =>
            (medicine.NDC != null && ndc11Candidates.Contains(medicine.NDC))
            || (medicine.NormalizedNdc != null && ndc11Candidates.Contains(medicine.NormalizedNdc))))
            .Select(medicine => ValidationHelperBase.NormalizeNdc(medicine.NDC) ?? medicine.NormalizedNdc)
            .Where(value =>
                !string.IsNullOrWhiteSpace(value)
                && value.Length == 11
                && value.All(char.IsAsciiDigit))
            .Cast<string>()
            .ToList();

        return matches.FirstOrDefault(value => value == preferredNdc11) ?? matches.FirstOrDefault();
    }

    private async Task<string?> FindPackageNdcAsync(
        IReadOnlyList<string> hyphenCandidates,
        CancellationToken cancellationToken)
    {
        if (hyphenCandidates.Count == 0)
            return null;

        var search = string.Join(
            "+",
            hyphenCandidates.Select(candidate =>
                $"packaging.package_ndc:{candidate}"));

        try
        {
            var url =
                $"drug/ndc.json?limit={Math.Min(hyphenCandidates.Count, 20)}&search={Uri.EscapeDataString(search)}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "openFDA NDC lookup failed with status {StatusCode}.",
                    response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("results", out var results))
                return null;

            foreach (var result in results.EnumerateArray())
            {
                if (!result.TryGetProperty("packaging", out var packaging))
                    continue;

                foreach (var package in packaging.EnumerateArray())
                {
                    if (!package.TryGetProperty("package_ndc", out var packageNdcElement))
                        continue;

                    var packageNdc = packageNdcElement.GetString();
                    if (packageNdc != null
                        && hyphenCandidates.Contains(packageNdc, StringComparer.Ordinal)
                        && TryConvertToNdc11(packageNdc, out var ndc11))
                    {
                        return ndc11;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("openFDA NDC lookup timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "openFDA NDC lookup could not be completed.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "openFDA returned an invalid NDC response.");
        }

        return null;
    }

    private static IReadOnlyList<string> BuildPackageNdcCandidates(string digits)
    {
        var candidates = new List<string>();
        var ndc11 = digits.PadLeft(11, '0');
        var labeler = ndc11[..5];
        var product = ndc11.Substring(5, 4);
        var package = ndc11.Substring(9, 2);

        if (labeler[0] == '0')
            AddCandidate(candidates, $"{labeler[1..]}-{product}-{package}");
        if (product[0] == '0')
            AddCandidate(candidates, $"{labeler}-{product[1..]}-{package}");
        if (package[0] == '0')
            AddCandidate(candidates, $"{labeler}-{product}-{package[1..]}");
        AddCandidate(candidates, $"{labeler}-{product}-{package}");

        var ndc10 = digits.PadLeft(10, '0');
        AddCandidate(candidates, $"{ndc10[..4]}-{ndc10.Substring(4, 4)}-{ndc10[8..]}");
        AddCandidate(candidates, $"{ndc10[..5]}-{ndc10.Substring(5, 3)}-{ndc10[8..]}");
        AddCandidate(candidates, $"{ndc10[..5]}-{ndc10.Substring(5, 4)}-{ndc10[9..]}");

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (!candidates.Contains(candidate, StringComparer.Ordinal))
            candidates.Add(candidate);
    }

    private static bool TryConvertToNdc11(string? packageNdc, out string ndc11)
    {
        ndc11 = string.Empty;
        var segments = packageNdc?.Split('-');
        if (segments is not { Length: 3 }
            || segments.Any(segment => !segment.All(char.IsAsciiDigit))
            || segments[0].Length > 5
            || segments[1].Length > 4
            || segments[2].Length > 2)
        {
            return false;
        }

        ndc11 = segments[0].PadLeft(5, '0')
            + segments[1].PadLeft(4, '0')
            + segments[2].PadLeft(2, '0');
        return ndc11.Length == 11;
    }
}
