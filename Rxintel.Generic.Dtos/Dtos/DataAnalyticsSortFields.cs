namespace Rxintel.Generic.Dtos.Dtos
{
    /// <summary>
    /// Sort keys for Data Analytics base grid columns.
    /// Keep in sync with <c>dataAnalyticsSort.ts</c> in the frontend.
    /// </summary>
    public static class DataAnalyticsSortFields
    {
        public const string Ndc = "ndc";
        public const string ProductName = "productName";
        public const string PackageSize = "packageSize";
        public const string Verified = "verified";
        public const string Source = "source";
        public const string TotalQuantityOrdered = "totalQuantityOrdered";
        public const string TotalBilledQuantity = "totalBilledQuantity";
        public const string NetQuantity = "netQuantity";
        public const string DollarsBilledTotal = "dollarsBilledTotal";

        public const string DefaultSortBy = DollarsBilledTotal;
        public const string DefaultSortDirection = "desc";
    }
}
