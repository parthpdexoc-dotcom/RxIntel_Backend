using System.Collections.Generic;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxComparisonEngine
    {
        /// <summary>
        /// Compares an audit record against a list of active rules and returns discrepancies found.
        /// </summary>
        List<ErxAuditDiscrepancy> CompareRecord(ErxAuditRecord record, IEnumerable<ErxColumnMatchRule> rules);

        /// <summary>
        /// Evaluates a single rule between two string values.
        /// </summary>
        bool EvaluateMatch(ErxColumnMatchRule rule, string? mmValue, string? erxValue);

        /// <summary>
        /// Evaluates whether Patient Names are clinically equivalent between MicroMerchant and ERX,
        /// handling suffixes (Jr, Sr, etc.), word order (Last First vs First Last), middle initials,
        /// and standard clinical nicknames/diminutives (e.g., Sam vs Sammy, Bill vs William).
        /// </summary>
        bool ArePatientNamesClinicallyEquivalent(string? mmName, string? erxName, ErxAuditRecord? record = null);

        /// <summary>
        /// Cross-validates Patient Demographics (DOB, Phone, Address) between MicroMerchant and ERX.
        /// Returns true if all three demographic fields match.
        /// </summary>
        bool ArePatientDemographicsMatching(ErxAuditRecord? record);

        /// <summary>
        /// Cross-validates NDC codes between MicroMerchant and ERX.
        /// Returns true if NDCs match (exact, stripped punctuation, normalized leading zeros, or 10-to-11 digit format).
        /// </summary>
        bool AreNdcsMatching(string? mmNdc, string? erxNdc);

        /// <summary>
        /// Cross-validates NDC codes for an audit record.
        /// </summary>
        bool AreNdcsMatching(ErxAuditRecord? record);

        /// <summary>
        /// Evaluates whether the Quantity Dispensed is less than or equal to Quantity Ordered.
        /// </summary>
        bool IsQuantityDispensedCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m);

        /// <summary>
        /// Evaluates whether the Quantity Ordered between MicroMerchant and ERX is clinically compliant,
        /// including clinical package size and unit-of-measure equivalence (e.g. 1 bottle vs 60 mL, 200 puffs vs 1 canister).
        /// </summary>
        bool IsQuantityOrderedCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m);

        /// <summary>
        /// Calculates the ERX Based Days Supply using Erx Qty as numerator and units per day from SIG as denominator.
        /// </summary>
        int? CalculateErxBasedDaysSupply(ErxAuditRecord? record);

        /// <summary>
        /// Evaluates whether Days Supply between MicroMerchant and ERX is clinically compliant based on SIG dosing.
        /// </summary>
        bool IsDaysSupplyCompliant(ErxAuditRecord? record, decimal tolerance = 0.00m);
    }
}
