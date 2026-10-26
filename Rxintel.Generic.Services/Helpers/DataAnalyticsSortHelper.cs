using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Helpers
{
    public static class DataAnalyticsSortHelper
    {
        public static bool IsDescending(string? sortDirection) =>
            string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        public static string NormalizeSortKey(string sortBy)
        {
            return sortBy.Trim().Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        }

        public static List<DataAnalyticsRowDto> SortRows(
            List<DataAnalyticsRowDto> rows,
            string? sortBy,
            string? sortDirection)
        {
            if (rows.Count <= 1 || string.IsNullOrWhiteSpace(sortBy))
                return rows;

            var descending = IsDescending(sortDirection);
            var key = NormalizeSortKey(sortBy);

            return key switch
            {
                "ndc" => OrderBy(rows, r => r.Ndc ?? "", descending, StringComparer.OrdinalIgnoreCase).ToList(),
                "productname" => OrderBy(rows, r => r.ProductName ?? "", descending, StringComparer.OrdinalIgnoreCase).ToList(),
                "packagesize" => OrderBy(rows, r => ParsePackageSizeSortKey(r.PackageSize), descending).ToList(),
                "verified" => OrderBy(rows, r => r.Verified ?? "", descending, StringComparer.OrdinalIgnoreCase).ToList(),
                "wholesaler" or "source" => OrderBy(rows, r => r.Source ?? "", descending, StringComparer.OrdinalIgnoreCase).ToList(),
                "quantityordered" or "totalquantityordered" => OrderBy(rows, r => r.TotalQuantityOrdered, descending).ToList(),
                "billedquantity" or "totalbilledquantity" => OrderBy(rows, r => r.TotalBilledQuantity, descending).ToList(),
                "netquantity" => OrderNetQuantityByDisplayedMagnitude(rows, descending),
                "dollarsbilled" or "dollarsbilledtotal" => OrderBy(rows, r => r.DollarsBilledTotal, descending).ToList(),
                _ => rows,
            };
        }

        /// <summary>
        /// Sort by the magnitude shown in the net quantity badge (absolute value), not signed surplus/shortage.
        /// </summary>
        private static List<DataAnalyticsRowDto> OrderNetQuantityByDisplayedMagnitude(
            List<DataAnalyticsRowDto> rows,
            bool descending)
        {
            static decimal Magnitude(DataAnalyticsRowDto r) => Math.Abs(r.NetQuantity);

            return descending
                ? rows.OrderByDescending(Magnitude).ThenByDescending(r => r.NetQuantity).ToList()
                : rows.OrderBy(Magnitude).ThenBy(r => r.NetQuantity).ToList();
        }

        private static decimal ParsePackageSizeSortKey(string? packageSize)
        {
            if (string.IsNullOrWhiteSpace(packageSize))
                return decimal.MinValue;

            return decimal.TryParse(packageSize.Trim(), out var parsed)
                ? parsed
                : decimal.MinValue;
        }

        private static IOrderedEnumerable<DataAnalyticsRowDto> OrderBy<T>(
            IEnumerable<DataAnalyticsRowDto> rows,
            Func<DataAnalyticsRowDto, T> keySelector,
            bool descending,
            IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            return descending
                ? rows.OrderByDescending(keySelector, comparer)
                : rows.OrderBy(keySelector, comparer);
        }
    }
}
