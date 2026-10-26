using System.Text.RegularExpressions;

namespace Rxintel.Generic.Common.Helpers;

/// <summary>
/// Common list helpers for in-memory search and filter building.
/// </summary>
public static class ListSearchHelper
{
    private static string NormalizeForSearch(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value.Trim().ToLowerInvariant(), @"[\s_]+", "");

    /// <summary>
    /// Filters a list when <paramref name="search"/> matches any of the given string properties (case-insensitive contains).
    /// Returns the original list unchanged when search is empty or no property getters are provided.
    /// </summary>
    public static List<T> FilterBySearch<T>(this List<T> list, string? search, params Func<T, string?>[] propertyGetters)
    {
        if (string.IsNullOrWhiteSpace(search) || propertyGetters.Length == 0)
            return list;
        var term = NormalizeForSearch(search);
        return list.Where(item => propertyGetters.Any(getter =>
        {
                var value = getter(item);
                return !string.IsNullOrWhiteSpace(value) && NormalizeForSearch(value).Contains(term);
            }))
            .ToList();
    } 
   
}
