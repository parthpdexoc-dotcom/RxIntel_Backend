using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Dtos.Dtos
{
    /// <summary>Query params from UI / Swagger.</summary>
    public class DataAnalyticsQueryParam
    {
        public Guid PharmacyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool SplitDates { get; set; }
        public DateTime? BillingStartDate { get; set; }
        public DateTime? BillingEndDate { get; set; }
        public DateTime? OrderedStartDate { get; set; }
        public DateTime? OrderedEndDate { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        /// <summary>Base grid column sort key (e.g. ndc, dollarsBilledTotal).</summary>
        public string? SortBy { get; set; }
        /// <summary>asc or desc (case-insensitive).</summary>
        public string? SortDirection { get; set; }
        public string? SearchText { get; set; }
        public List<string>? SelectedColumns { get; set; }
        public List<string>? HiddenColumns { get; set; }
        public List<string>? FilterNdcs { get; set; }
        /// <summary>When true with FilterNdcs, match only exact NDCs. When false, include related NDCs (strength/brand/dosage/package or product+package).</summary>
        public bool NdcSpecified { get; set; }
        public List<string>? FilterProductNames { get; set; }
        public List<string>? FilterPackageSizes { get; set; }
        public List<string>? FilterInsuranceNames { get; set; }
        /// <summary>
        /// False (Simple View): parent/child insurance family totals.
        /// True (Advanced View): actual billed quantity and net per insurance, no family duplication.
        /// </summary>
        public bool AdvancedView { get; set; }
        /// <summary>Filter rows by net quantity badge (surplus / shortage).</summary>
        public NetQuantityFilter NetQuantityFilter { get; set; } = NetQuantityFilter.All;
    }

    /// <summary>One row per NDC in the report.</summary>
    public class DataAnalyticsRowDto
    {
        // ── Medicine (Verified/Source calculated, not in DB) ──
        public string Ndc { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string? PackageSize { get; set; }
        public string? Strength { get; set; }
        public string? StrengthUnit { get; set; }
        public string? DosageForm { get; set; }
        public string? Brand { get; set; }
        public string Verified { get; set; } = "No";
        public string Source { get; set; } = "GSDD";

        // ── Totals ──
        public decimal TotalQuantityOrdered { get; set; }
        public decimal TotalBilledQuantity { get; set; }
        public decimal NetQuantity { get; set; }
        public decimal DollarsBilledTotal { get; set; }

        /// <summary>Quantity ordered per wholesaler for this NDC (same units as TotalQuantityOrdered).</summary>
        public Dictionary<string, decimal> WholesalerQuantities { get; set; } = new();

        // ── Dynamic sections (key = insurance name) ──
        public Dictionary<string, decimal> PrimaryInsurancePackages { get; set; } = new();
        public Dictionary<string, decimal> PrimaryInsuranceNet { get; set; } = new();
        public Dictionary<string, decimal> PrimaryInsuranceDollars { get; set; } = new();
        /// <summary>Actual billed packages per insurance, without parent/child family rollup.</summary>
        public Dictionary<string, decimal> ActualInsurancePackages { get; set; } = new();
        /// <summary>Net packages using actual billed quantity per insurance.</summary>
        public Dictionary<string, decimal> ActualInsuranceNet { get; set; } = new();
    }

    public class DataAnalyticsResponseDto
    {
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public List<string> PrimaryInsuranceColumnNames { get; set; } = new();
        public List<InsuranceFamilyDto> InsuranceFamilies { get; set; } = new();
        /// <summary>Wholesaler names present in the pharmacy-filtered purchases for this report.</summary>
        public List<string> WholesalerColumnNames { get; set; } = new();

        /// <summary>NDC values available for filter dropdowns (after net-quantity filter, before other secondary filters).</summary>
        public List<string> FilterNdcOptions { get; set; } = new();
        public List<string> FilterProductNameOptions { get; set; } = new();
        public List<string> FilterPackageSizeOptions { get; set; } = new();

        public List<DataAnalyticsRowDto> Data { get; set; } = new();
    }

    public class InsuranceFamilyDto
    {
        public string ParentName { get; set; } = "";
        public List<string> MemberNames { get; set; } = new();
    }
}