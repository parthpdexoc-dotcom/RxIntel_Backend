using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Resolves connected insurance families from InsuranceGroup links and rolls up
    /// billing totals so every family member shares the same total.
    /// A link is identified by BIN number → child insurance ID and is only active when
    /// BillDate falls within the link StartDate–EndDate (inclusive).
    /// A null EndDate means the link remains active indefinitely.
    /// Outside that range, billing is treated as independent.
    /// </summary>
    public class InsuranceFamilyAggregator
    {
        private readonly List<InsuranceGroup> _links;
        private readonly Dictionary<string, List<Guid>> _insuranceIdsByBin;

        public InsuranceFamilyAggregator(
            IEnumerable<InsuranceGroup> links,
            IEnumerable<Insurance> insurances)
        {
            _links = links
                .Where(l => !string.IsNullOrWhiteSpace(l.BINNumber) && l.ChildInsuranceId != Guid.Empty)
                .ToList();

            _insuranceIdsByBin = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            foreach (var insurance in insurances)
            {
                var bin = NormalizeBinKey(insurance.BINNumber);
                if (bin == null)
                    continue;

                if (!_insuranceIdsByBin.TryGetValue(bin, out var ids))
                {
                    ids = [];
                    _insuranceIdsByBin[bin] = ids;
                }

                ids.Add(insurance.Id);
            }
        }

        public Dictionary<Guid, decimal> ApplyFamilyTotals(
            IEnumerable<Billing> billings,
            Func<Billing, decimal> selector)
        {
            var billingList = billings
                .Where(b => b.InsuranceId != Guid.Empty)
                .ToList();

            var insuranceIds = new HashSet<Guid>();
            foreach (var link in _links)
            {
                insuranceIds.Add(link.ChildInsuranceId);
                foreach (var parentId in GetInsuranceIdsForBin(link.BINNumber))
                    insuranceIds.Add(parentId);
            }

            foreach (var billing in billingList)
                insuranceIds.Add(billing.InsuranceId);

            var result = insuranceIds.ToDictionary(id => id, _ => 0m);

            foreach (var dateGroup in billingList.GroupBy(b => b.BillDate.Date))
            {
                var parent = BuildParentMapForDate(dateGroup.Key);

                var totalsByRoot = new Dictionary<Guid, decimal>();
                foreach (var billing in dateGroup)
                {
                    var root = Find(parent, billing.InsuranceId);
                    totalsByRoot[root] = totalsByRoot.GetValueOrDefault(root) + selector(billing);
                }

                foreach (var insuranceId in insuranceIds)
                {
                    var root = Find(parent, insuranceId);
                    if (totalsByRoot.TryGetValue(root, out var familyTotal))
                        result[insuranceId] += familyTotal;
                }
            }

            return result;
        }

        private Dictionary<Guid, Guid> BuildParentMapForDate(DateTime billDate)
        {
            var parent = new Dictionary<Guid, Guid>();

            foreach (var link in _links)
            {
                if (!IsLinkActiveOn(link, billDate))
                    continue;

                foreach (var parentId in GetInsuranceIdsForBin(link.BINNumber))
                    Union(parent, parentId, link.ChildInsuranceId);
            }

            return parent;
        }

        private static bool IsLinkActiveOn(InsuranceGroup link, DateTime billDate)
        {
            if (!link.StartDate.HasValue)
                return false;

            var date = billDate.Date;
            if (date < link.StartDate.Value.Date)
                return false;

            return !link.EndDate.HasValue || date <= link.EndDate.Value.Date;
        }

        private IEnumerable<Guid> GetInsuranceIdsForBin(string? binNumber)
        {
            var bin = NormalizeBinKey(binNumber);
            if (bin == null)
                return [];

            return _insuranceIdsByBin.TryGetValue(bin, out var ids) ? ids : [];
        }

        /// <summary>
        /// Matches insurance rows to group links regardless of leading-zero BIN formatting.
        /// </summary>
        public static string? NormalizeBinKey(string? binNumber)
        {
            var trimmed = binNumber?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return null;

            if (trimmed.Length >= BillingValidationHelper.BinLength
                || !trimmed.All(char.IsAsciiDigit))
                return trimmed;

            return trimmed.PadLeft(BillingValidationHelper.BinLength, '0');
        }

        private static Guid Find(Dictionary<Guid, Guid> parent, Guid id)
        {
            if (!parent.ContainsKey(id))
                parent[id] = id;

            if (parent[id] != id)
                parent[id] = Find(parent, parent[id]);

            return parent[id];
        }

        private static void Union(Dictionary<Guid, Guid> parent, Guid a, Guid b)
        {
            var rootA = Find(parent, a);
            var rootB = Find(parent, b);
            if (rootA != rootB)
                parent[rootB] = rootA;
        }
    }
}
