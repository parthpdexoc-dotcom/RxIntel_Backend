using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Purchase CSV calculated-field helpers.
    /// </summary>
    public static class PurchaseCsvHelper
    {
        /// <summary>
        /// Normalizes NDC for storage without hyphens. Hyphenated values are padded
        /// to 5-4-2 first (e.g. 1234-5678-90 → 01234567890).
        /// </summary>
        public static void ApplyCalculatedFields(PurchaseDto? dto)
        {
            if (dto == null)
                return;

            dto.NDC = ValidationHelperBase.NormalizeNdc(dto.NDC) ?? dto.NDC;
            dto.TotalQuantityOrdered = PurchaseValidationHelper.CalculateTotalQuantityOrdered(
                dto.QuantityPurchased,
                dto.PackageSize);
        }
    }
}
