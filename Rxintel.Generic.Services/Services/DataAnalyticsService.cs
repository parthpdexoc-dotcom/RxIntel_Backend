using System.Linq.Expressions;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Helpers;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class DataAnalyticsService : IDataAnalyticsService
    {
        private static readonly Expression<Func<Billing, object>>[] BillingIncludes =
            [b => b.Insurance!];

        private static readonly Expression<Func<Purchase, object>>[] PurchaseIncludes =
            [p => p.Wholesaler!];

        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IBillingRepository _billingRepository;
        private readonly IMedicineRepository _medicineRepository;
        private readonly IInsuranceRepository _insuranceRepository;
        private readonly IInsuranceGroupRepository _insuranceGroupRepository;
        private readonly ITokenParser _tokenParser;

        public DataAnalyticsService(
            IPurchaseRepository purchaseRepository,
            IBillingRepository billingRepository,
            IMedicineRepository medicineRepository,
            IInsuranceRepository insuranceRepository,
            IInsuranceGroupRepository insuranceGroupRepository,
            ITokenParser tokenParser)
        {
            _purchaseRepository = purchaseRepository;
            _billingRepository = billingRepository;
            _medicineRepository = medicineRepository;
            _insuranceRepository = insuranceRepository;
            _insuranceGroupRepository = insuranceGroupRepository;
            _tokenParser = tokenParser;
        }

        public async Task<DataAnalyticsResponseDto> GetAnalyticsAsync(DataAnalyticsQueryParam param)
        {
            var pharmacyId = _tokenParser.GetPharmacyId() ?? param.PharmacyId;
            if (pharmacyId == Guid.Empty)
                return EmptyResponse(param);

            var (billStart, billEnd) = ResolveBillingRange(param);
            var page = param.Page is > 0 ? param.Page.Value : 1;
            var pageSize = param.PageSize is > 0 ? param.PageSize.Value : 10;

            var insurances = (await _insuranceRepository.GetAllAsync()).ToList();
            var primaryInsuranceNames = insurances
                .Where(i => !string.IsNullOrWhiteSpace(i.InsuranceName))
                .GroupBy(i => i.InsuranceName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(i => i.BINNumber).First().InsuranceName!.Trim())
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var insuranceIdsByName = insurances
                .Where(i => !string.IsNullOrWhiteSpace(i.InsuranceName))
                .GroupBy(i => i.InsuranceName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => i.Id).Distinct().ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var insuranceNameById = insurances
                .Where(i => i.Id != Guid.Empty && !string.IsNullOrWhiteSpace(i.InsuranceName))
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First().InsuranceName!.Trim());

            var insuranceLinks = (await _insuranceGroupRepository.GetAllAsync()).ToList();
            var familyAggregator = new InsuranceFamilyAggregator(insuranceLinks, insurances);
            var insuranceFamilies = BuildInsuranceFamilies(insuranceLinks, insurances, insuranceNameById);

            var billings = (await _billingRepository.GetAllAsync(
                b => b.PharmacyId == pharmacyId
                  && b.BillDate >= billStart
                  && b.BillDate <= billEnd,
                includes: BillingIncludes
            )).ToList();

            List<Purchase> purchases;
            List<string> ndcList;

            if (param.SplitDates)
            {
                var (orderStart, orderEnd) = ResolveOrderedRange(param);

                purchases = (await _purchaseRepository.GetAllAsync(
                    p => p.PharmacyId == pharmacyId
                      && p.InvoiceDate != null
                      && p.InvoiceDate >= orderStart
                      && p.InvoiceDate <= orderEnd,
                    includes: PurchaseIncludes
                )).ToList();

                ndcList = purchases.Select(p => p.NDC)
                    .Union(billings.Select(b => b.NDC))
                    .Where(ndc => !string.IsNullOrWhiteSpace(ndc))
                    .Select(ndc => ndc.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                ndcList = billings.Select(b => b.NDC)
                    .Where(ndc => !string.IsNullOrWhiteSpace(ndc))
                    .Select(ndc => ndc.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (ndcList.Count == 0)
                    return EmptyResponse(param, primaryInsuranceNames, insuranceFamilies);

                purchases = (await _purchaseRepository.GetAllAsync(
                    p => p.PharmacyId == pharmacyId && ndcList.Contains(p.NDC),
                    includes: PurchaseIncludes
                )).ToList();
            }

            if (ndcList.Count == 0)
                return EmptyResponse(param, primaryInsuranceNames, insuranceFamilies);

            // Index-friendly lookup: match on NormalizedNdc (hyphens stripped) instead of
            // SQL REPLACE on every Medicine row, which timed out after the full product import.
            var normalizedSearchNdcs = ndcList
                .Select(n => n.Replace("-", "").Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allMatchingMedicines = (await _medicineRepository.GetAllAsync(
                    m => m.NormalizedNdc != null && normalizedSearchNdcs.Contains(m.NormalizedNdc)))
                .Where(m => !string.IsNullOrWhiteSpace(m.NDC))
                .ToList();

            var medicinesByNdc = new Dictionary<string, Medicine>(StringComparer.OrdinalIgnoreCase);
            foreach (var med in allMatchingMedicines)
            {
                var raw = med.NDC?.Trim();
                if (!string.IsNullOrEmpty(raw))
                {
                    medicinesByNdc.TryAdd(raw, med);
                    var stripped = raw.Replace("-", "");
                    medicinesByNdc.TryAdd(stripped, med);
                }

                var normalized = med.NormalizedNdc?.Trim();
                if (!string.IsNullOrEmpty(normalized))
                    medicinesByNdc.TryAdd(normalized, med);
            }

            if (!string.IsNullOrWhiteSpace(param.SearchText))
            {
                var search = param.SearchText.Trim().ToLowerInvariant();
                ndcList = ndcList.Where(ndc =>
                {
                    var med = LookupMedicine(medicinesByNdc, ndc);
                    return ndc.ToLowerInvariant().Contains(search)
                        || BuildProductName(med).ToLowerInvariant().Contains(search);
                }).ToList();
            }

            if (ndcList.Count == 0)
                return EmptyResponse(param, primaryInsuranceNames, insuranceFamilies);

            var purchasesByNdc = GroupPurchasesByNdc(purchases);
            var billingsByNdc = GroupBillingsByNdc(billings);
            var wholesalerColumnNames = BuildWholesalerColumnNames(purchases);

            var rows = ndcList.Select(ndc => BuildRow(
                ndc,
                LookupMedicine(medicinesByNdc, ndc),
                purchasesByNdc.GetValueOrDefault(ndc) ?? [],
                billingsByNdc.GetValueOrDefault(ndc) ?? [],
                primaryInsuranceNames,
                wholesalerColumnNames,
                familyAggregator,
                insuranceIdsByName,
                insuranceNameById,
                useActualInsuranceTotals: param.AdvancedView
            )).ToList();

            // Cascade facet options:
            // - NDC options: full report set (never narrowed by Product/Package, so selecting
            //   a product cannot drop user-selected NDCs)
            // - Product options: constrained by selected NDCs (+ related) + Package + Insurance + Net Qty
            // - Package options: constrained by selected NDCs (+ related) + Product + Insurance + Net Qty
            var filterNdcOptions = BuildDistinctValues(rows, r => r.Ndc);

            var filterProductNameOptions = BuildDistinctValues(
                ApplyNetQuantityFilter(
                    ApplySecondaryFilters(rows, param, applyNdc: true, applyProduct: false, applyPackage: true, applyInsurance: true),
                    param.NetQuantityFilter),
                r => r.ProductName);

            var filterPackageSizeOptions = BuildDistinctValues(
                ApplyNetQuantityFilter(
                    ApplySecondaryFilters(rows, param, applyNdc: true, applyProduct: true, applyPackage: false, applyInsurance: true),
                    param.NetQuantityFilter),
                r => r.PackageSize,
                numericSort: true);

            // Grid rows: NDC (+ related) → Product → Package → Insurance → Net Quantity
            rows = ApplySecondaryFilters(rows, param);
            rows = ApplyNetQuantityFilter(rows, param.NetQuantityFilter);

            rows = DataAnalyticsSortHelper.SortRows(rows, param.SortBy, param.SortDirection);

            return new DataAnalyticsResponseDto
            {
                TotalRecords = rows.Count,
                Page = page,
                PageSize = pageSize,
                PrimaryInsuranceColumnNames = primaryInsuranceNames,
                InsuranceFamilies = insuranceFamilies,
                WholesalerColumnNames = wholesalerColumnNames,
                FilterNdcOptions = filterNdcOptions,
                FilterProductNameOptions = filterProductNameOptions,
                FilterPackageSizeOptions = filterPackageSizeOptions,
                Data = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public async Task<byte[]> ExportAnalyticsAsync(DataAnalyticsQueryParam param)
        {
            param.Page = 1;
            param.PageSize = int.MaxValue;
            var analytics = await GetAnalyticsAsync(param);
            return new DataAnalyticsExcelExporter().Export(
                analytics,
                param.SelectedColumns,
                param.HiddenColumns,
                param.AdvancedView);
        }

        private static List<DataAnalyticsRowDto> ApplyNetQuantityFilter(
            List<DataAnalyticsRowDto> rows,
            NetQuantityFilter filter) =>
            filter switch
            {
                NetQuantityFilter.Surplus => rows.Where(r => r.NetQuantity >= 0).ToList(),
                NetQuantityFilter.Shortage => rows.Where(r => r.NetQuantity < 0).ToList(),
                _ => rows,
            };

        private static List<string> BuildDistinctValues(
            IEnumerable<DataAnalyticsRowDto> rows,
            Func<DataAnalyticsRowDto, string?> selector,
            bool numericSort = false)
        {
            var values = rows
                .Select(r => (selector(r) ?? "").Trim())
                .Where(v => v.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return SortFacetValues(values, numericSort);
        }

        private static List<string> SortFacetValues(IEnumerable<string> values, bool numericSort)
        {
            if (!numericSort)
                return values.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

            return values
                .OrderBy(v => decimal.TryParse(v, out var n) ? n : decimal.MaxValue)
                .ThenBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<DataAnalyticsRowDto> ApplySecondaryFilters(
            List<DataAnalyticsRowDto> rows,
            DataAnalyticsQueryParam param,
            bool applyNdc = true,
            bool applyProduct = true,
            bool applyPackage = true,
            bool applyInsurance = true)
        {
            // Exact user-selected NDCs are sticky: Product/Package may narrow related
            // expansions, but must never drop an NDC the user explicitly selected.
            var exactSelectedNdcs = new HashSet<string>(
                (param.FilterNdcs ?? [])
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (applyNdc && exactSelectedNdcs.Count > 0)
            {
                if (param.NdcSpecified)
                {
                    rows = rows.Where(r => exactSelectedNdcs.Contains((r.Ndc ?? "").Trim())).ToList();
                }
                else
                {
                    rows = ExpandRelatedNdcRows(rows, exactSelectedNdcs);
                }
            }

            if (applyProduct && param.FilterProductNames != null && param.FilterProductNames.Count > 0)
            {
                var productSet = new HashSet<string>(
                    param.FilterProductNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(r =>
                    exactSelectedNdcs.Contains((r.Ndc ?? "").Trim()) ||
                    productSet.Contains((r.ProductName ?? "").Trim())).ToList();
            }

            if (applyPackage && param.FilterPackageSizes != null && param.FilterPackageSizes.Count > 0)
            {
                var packageSet = new HashSet<string>(
                    param.FilterPackageSizes.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(r =>
                    exactSelectedNdcs.Contains((r.Ndc ?? "").Trim()) ||
                    packageSet.Contains((r.PackageSize ?? "").Trim())).ToList();
            }

            if (applyInsurance && param.FilterInsuranceNames != null && param.FilterInsuranceNames.Count > 0)
            {
                rows = rows.Where(r => RowPassesInsuranceFilter(r, param.FilterInsuranceNames, param.AdvancedView)).ToList();
            }

            return rows;
        }

        private static List<DataAnalyticsRowDto> ExpandRelatedNdcRows(
            List<DataAnalyticsRowDto> rows,
            HashSet<string> selectedNdcs)
        {
            var associatedContentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var associatedProductKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var ndc = (row.Ndc ?? "").Trim();
                if (!selectedNdcs.Contains(ndc))
                    continue;

                if (!string.IsNullOrWhiteSpace(row.Strength)
                    || !string.IsNullOrWhiteSpace(row.Brand)
                    || !string.IsNullOrWhiteSpace(row.DosageForm))
                {
                    associatedContentKeys.Add(GetContentKey(row));
                }

                var productName = (row.ProductName ?? "").Trim();
                if (productName.Length > 0)
                {
                    associatedProductKeys.Add(
                        $"{productName.ToLowerInvariant()}|{(row.PackageSize ?? "").Trim().ToLowerInvariant()}");
                }
            }

            return rows.Where(row =>
            {
                var ndc = (row.Ndc ?? "").Trim();
                if (selectedNdcs.Contains(ndc))
                    return true;

                var hasContent =
                    !string.IsNullOrWhiteSpace(row.Strength)
                    || !string.IsNullOrWhiteSpace(row.Brand)
                    || !string.IsNullOrWhiteSpace(row.DosageForm);
                if (hasContent && associatedContentKeys.Contains(GetContentKey(row)))
                    return true;

                var productKey =
                    $"{(row.ProductName ?? "").Trim().ToLowerInvariant()}|{(row.PackageSize ?? "").Trim().ToLowerInvariant()}";
                return associatedProductKeys.Contains(productKey);
            }).ToList();
        }

        private static string GetContentKey(DataAnalyticsRowDto row) =>
            $"{(row.Strength ?? "").Trim().ToLowerInvariant()}|" +
            $"{(row.Brand ?? "").Trim().ToLowerInvariant()}|" +
            $"{(row.DosageForm ?? "").Trim().ToLowerInvariant()}|" +
            $"{(row.PackageSize ?? "").Trim().ToLowerInvariant()}";

        private static bool RowPassesInsuranceFilter(
            DataAnalyticsRowDto row,
            List<string> selectedInsurances,
            bool advancedView)
        {
            foreach (var ins in selectedInsurances)
            {
                if (string.IsNullOrWhiteSpace(ins))
                    continue;

                var packages = advancedView ? row.ActualInsurancePackages : row.PrimaryInsurancePackages;
                var pkgs = GetDecimalByInsuranceName(packages, ins);
                var dollars = GetDecimalByInsuranceName(row.PrimaryInsuranceDollars, ins);
                if (pkgs > 0 || dollars > 0)
                    return true;
            }

            return false;
        }

        private static decimal GetDecimalByInsuranceName(
            Dictionary<string, decimal> map,
            string insuranceName)
        {
            if (map.TryGetValue(insuranceName, out var exact))
                return exact;

            foreach (var (key, value) in map)
            {
                if (string.Equals(key, insuranceName, StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            return 0;
        }

        private static Dictionary<string, List<Purchase>> GroupPurchasesByNdc(IEnumerable<Purchase> items) =>
            items.Where(p => !string.IsNullOrWhiteSpace(p.NDC))
                 .GroupBy(p => p.NDC.Trim(), StringComparer.OrdinalIgnoreCase)
                 .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, List<Billing>> GroupBillingsByNdc(IEnumerable<Billing> items) =>
            items.Where(b => !string.IsNullOrWhiteSpace(b.NDC))
                 .GroupBy(b => b.NDC.Trim(), StringComparer.OrdinalIgnoreCase)
                 .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        private static List<string> BuildWholesalerColumnNames(IEnumerable<Purchase> purchases) =>
            purchases
                .Select(GetWholesalerDisplayName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static string GetWholesalerDisplayName(Purchase purchase)
        {
            var name = purchase.Wholesaler?.WholesalerName?.Trim();
            return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }

        private static DataAnalyticsRowDto BuildRow(
            string ndc,
            Medicine? medicine,
            IReadOnlyList<Purchase> ndcPurchases,
            IReadOnlyList<Billing> ndcBillings,
            IReadOnlyList<string> primaryInsuranceNames,
            IReadOnlyList<string> wholesalerColumnNames,
            InsuranceFamilyAggregator familyAggregator,
            IReadOnlyDictionary<string, List<Guid>> insuranceIdsByName,
            IReadOnlyDictionary<Guid, string> insuranceNameById,
            bool useActualInsuranceTotals)
        {
            var isVerified = medicine != null;
            var productName = FirstNonEmpty(
                medicine != null ? BuildProductName(medicine) : null,
                GetFirstNonEmptyValue(ndcBillings, b => b.DrugName),
                GetFirstNonEmptyValue(ndcPurchases, p => p.Description));
            var packageSize = FirstNonEmpty(
                medicine?.PackageSize,
                GetFirstNonEmptyValue(ndcBillings, b => b.PackageSize),
                GetFirstNonEmptyValue(ndcPurchases, p => p.PackageSize));

            var totalQuantityOrdered = ndcPurchases.Sum(PurchaseValidationHelper.GetEffectiveTotalQuantityOrdered);
            var totalBilledQuantity = ndcBillings.Sum(b => b.QuantityBilled);
            var quantitiesByWholesaler = ndcPurchases
                .GroupBy(GetWholesalerDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(PurchaseValidationHelper.GetEffectiveTotalQuantityOrdered),
                    StringComparer.OrdinalIgnoreCase);

            var row = new DataAnalyticsRowDto
            {
                Ndc = ndc,
                ProductName = productName ?? "",
                PackageSize = packageSize,
                Strength = medicine?.Strength,
                StrengthUnit = medicine?.StrengthUnit,
                DosageForm = medicine?.DosageForm,
                Brand = medicine?.Brand,
                Verified = isVerified ? "Yes" : "No",
                Source = isVerified ? "GSDD" : "PMS",
                TotalQuantityOrdered = totalQuantityOrdered,
                TotalBilledQuantity = totalBilledQuantity,
                NetQuantity = totalQuantityOrdered - totalBilledQuantity,
                DollarsBilledTotal = ndcBillings.Sum(b => b.TotalInsurancePaidAmount ?? 0),
            };

            foreach (var wholesalerName in wholesalerColumnNames)
                row.WholesalerQuantities[wholesalerName] =
                    quantitiesByWholesaler.GetValueOrDefault(wholesalerName);

            var primaryBillingsByInsurer = ndcBillings
                .Where(b => !string.IsNullOrWhiteSpace(b.Insurance?.InsuranceName))
                .GroupBy(b => b.Insurance!.InsuranceName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var familyPackageTotals = familyAggregator.ApplyFamilyTotals(
                ndcBillings,
                b => b.QuantityBilled);

            var actualPackagesByName = SumActualPackagesByInsuranceName(ndcBillings, insuranceNameById);

            foreach (var insName in primaryInsuranceNames)
            {
                var insBillings = primaryBillingsByInsurer.GetValueOrDefault(insName) ?? [];
                var actualBilledQuantity = actualPackagesByName.GetValueOrDefault(insName);
                var familyBilledQuantity = ResolveFamilyBilledQuantity(
                    insName,
                    insuranceIdsByName,
                    familyPackageTotals,
                    actualBilledQuantity);

                var billedQuantity = useActualInsuranceTotals ? actualBilledQuantity : familyBilledQuantity;
                var quantityBaselineForNet = DataAnalyticsOptumRxHelper.GetQuantityOrderedBaselineForNet(
                    insName,
                    totalQuantityOrdered,
                    ndcPurchases);

                row.PrimaryInsurancePackages[insName] = billedQuantity;
                row.PrimaryInsuranceDollars[insName] = insBillings.Sum(b => b.PrimaryInsurancePaidAmount);
                // Excess quantities always follow family rollup (same as Simple View), even in Advanced View.
                row.PrimaryInsuranceNet[insName] = quantityBaselineForNet - familyBilledQuantity;
                row.ActualInsurancePackages[insName] = actualBilledQuantity;
                row.ActualInsuranceNet[insName] = actualBilledQuantity == 0
                    ? 0
                    : quantityBaselineForNet - actualBilledQuantity;
            }

            return row;
        }

        /// <summary>
        /// Simple View: linked family members share one rolled-up billed quantity per name.
        /// Uses the highest family total among all insurance IDs for that name (multi-BIN carriers).
        /// </summary>
        private static decimal ResolveFamilyBilledQuantity(
            string insName,
            IReadOnlyDictionary<string, List<Guid>> insuranceIdsByName,
            IReadOnlyDictionary<Guid, decimal> familyPackageTotals,
            decimal actualBilledQuantity)
        {
            if (!insuranceIdsByName.TryGetValue(insName, out var ids) || ids.Count == 0)
                return actualBilledQuantity;

            var maxFamilyTotal = ids.Max(id => familyPackageTotals.GetValueOrDefault(id));
            return maxFamilyTotal > 0 ? maxFamilyTotal : actualBilledQuantity;
        }

        private static Dictionary<string, decimal> SumActualPackagesByInsuranceName(
            IReadOnlyList<Billing> ndcBillings,
            IReadOnlyDictionary<Guid, string> insuranceNameById)
        {
            var actualPackagesByName = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var billing in ndcBillings)
            {
                var insuranceName = billing.Insurance?.InsuranceName?.Trim();
                if (string.IsNullOrWhiteSpace(insuranceName)
                    && billing.InsuranceId != Guid.Empty)
                {
                    insuranceNameById.TryGetValue(billing.InsuranceId, out insuranceName);
                }

                if (string.IsNullOrWhiteSpace(insuranceName))
                    continue;

                actualPackagesByName[insuranceName] =
                    actualPackagesByName.GetValueOrDefault(insuranceName) + billing.QuantityBilled;
            }

            return actualPackagesByName;
        }

        private static Medicine? LookupMedicine(Dictionary<string, Medicine> dict, string ndc)
        {
            if (string.IsNullOrWhiteSpace(ndc)) return null;
            if (dict.TryGetValue(ndc.Trim(), out var med)) return med;
            var stripped = ndc.Replace("-", "").Trim();
            if (dict.TryGetValue(stripped, out med)) return med;
            return null;
        }

        private static string BuildProductName(Medicine? m)
        {
            if (m == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(m.Brand))
                parts.Add(m.Brand.Trim());

            if (!string.IsNullOrWhiteSpace(m.Generic))
                parts.Add(m.Generic.Trim());

            var strength = m.Strength?.Trim() ?? "";
            var strengthUnit = m.StrengthUnit?.Trim() ?? "";
            var strengthPart = $"{strength}{strengthUnit}".Trim();
            if (!string.IsNullOrWhiteSpace(strengthPart))
                parts.Add(strengthPart);

            if (!string.IsNullOrWhiteSpace(m.DosageForm))
                parts.Add(m.DosageForm.Trim());

            var result = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(result) ? (m.Brand?.Trim() ?? "") : result;
        }

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

        private static string? GetFirstNonEmptyValue<T>(
            IEnumerable<T> items,
            Func<T, string?> selector) =>
            items
                .Select(selector)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                ?.Trim();

        private static (DateTime Start, DateTime End) ToInclusiveRange(DateTime startDate, DateTime endDate) =>
            (startDate.Date, endDate.Date.AddDays(1).AddTicks(-1));

        private static (DateTime Start, DateTime End) ResolveOrderedRange(DataAnalyticsQueryParam param) =>
            ToInclusiveRange(param.OrderedStartDate!.Value, param.OrderedEndDate!.Value);

        private static (DateTime Start, DateTime End) ResolveBillingRange(DataAnalyticsQueryParam param) =>
            param.SplitDates
                ? ToInclusiveRange(param.BillingStartDate!.Value, param.BillingEndDate!.Value)
                : ToInclusiveRange(param.StartDate, param.EndDate);

        private static List<InsuranceFamilyDto> BuildInsuranceFamilies(
            IReadOnlyList<InsuranceGroup> links,
            IReadOnlyList<Insurance> insurances,
            IReadOnlyDictionary<Guid, string> insuranceNameById)
        {
            var validLinks = links
                .Where(l => !string.IsNullOrWhiteSpace(l.BINNumber) && l.ChildInsuranceId != Guid.Empty)
                .ToList();

            var childIds = validLinks.Select(l => l.ChildInsuranceId).ToHashSet();
            var idsByBin = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            foreach (var insurance in insurances)
            {
                var bin = InsuranceFamilyAggregator.NormalizeBinKey(insurance.BINNumber);
                if (bin == null)
                    continue;

                if (!idsByBin.TryGetValue(bin, out var ids))
                {
                    ids = [];
                    idsByBin[bin] = ids;
                }

                ids.Add(insurance.Id);
            }

            var families = new List<InsuranceFamilyDto>();

            foreach (var binGroup in validLinks.GroupBy(
                         l => InsuranceFamilyAggregator.NormalizeBinKey(l.BINNumber) ?? string.Empty,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(binGroup.Key))
                    continue;

                var parentIds = idsByBin.GetValueOrDefault(binGroup.Key) ?? [];
                var parentId = parentIds.FirstOrDefault(id => !childIds.Contains(id));
                if (parentId == Guid.Empty)
                    parentId = parentIds.FirstOrDefault();

                if (parentId == Guid.Empty
                    || !insuranceNameById.TryGetValue(parentId, out var parentName)
                    || string.IsNullOrWhiteSpace(parentName))
                    continue;

                var memberNames = new List<string> { parentName };
                foreach (var childId in binGroup.Select(l => l.ChildInsuranceId).Distinct())
                {
                    if (!insuranceNameById.TryGetValue(childId, out var childName)
                        || string.IsNullOrWhiteSpace(childName))
                        continue;

                    if (!memberNames.Exists(n => string.Equals(n, childName, StringComparison.OrdinalIgnoreCase)))
                        memberNames.Add(childName);
                }

                if (memberNames.Count < 2)
                    continue;

                families.Add(new InsuranceFamilyDto
                {
                    ParentName = parentName,
                    MemberNames = memberNames
                });
            }

            return families
                .GroupBy(f => f.ParentName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(f => f.ParentName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DataAnalyticsResponseDto EmptyResponse(
            DataAnalyticsQueryParam param,
            List<string>? primary = null,
            List<InsuranceFamilyDto>? families = null,
            List<string>? wholesalerNames = null) =>
            new()
            {
                TotalRecords = 0,
                Page = param.Page ?? 1,
                PageSize = param.PageSize ?? 10,
                PrimaryInsuranceColumnNames = primary ?? [],
                InsuranceFamilies = families ?? [],
                WholesalerColumnNames = wholesalerNames ?? [],
                Data = []
            };

    }
}
