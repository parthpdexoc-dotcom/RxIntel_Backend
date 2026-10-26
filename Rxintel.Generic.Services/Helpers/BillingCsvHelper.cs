using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Billing CSV BIN resolution and calculated-field helpers.
    /// </summary>
    public static class BillingCsvHelper
    {
        /// <summary>
        /// Normalizes NDC for storage without hyphens. Hyphenated values are padded
        /// to 5-4-2 first (e.g. 1234-5678-90 → 01234567890).
        /// </summary>
        public static void ApplyCalculatedFields(BillingDto? dto)
        {
            if (dto == null)
                return;

            dto.NDC = ValidationHelperBase.NormalizeNdc(dto.NDC) ?? dto.NDC;
            dto.TotalInsurancePaidAmount = BillingValidationHelper.CalculateTotalInsurancePaidAmount(
                dto.PrimaryInsurancePaidAmount,
                dto.SecondaryInsurancePaidAmount);
        }

        /// <summary>
        /// Pharmacy systems export BINs as plain numbers, so a BIN such as 017010 arrives as
        /// 17010 with its leading zeros dropped. Restore them before validation.
        /// </summary>
        public static void NormalizeBinNumbers(BillingDto? dto)
        {
            if (dto == null)
                return;

            dto.PrimaryInsuranceBinNo = PadBin(dto.PrimaryInsuranceBinNo);
            dto.SecondaryInsuranceBinNo = PadBin(dto.SecondaryInsuranceBinNo);
        }

        private static string? PadBin(string? bin)
        {
            var trimmed = bin?.Trim();

            if (string.IsNullOrEmpty(trimmed)
                || trimmed.Length >= BillingValidationHelper.BinLength
                || !trimmed.All(char.IsAsciiDigit))
                return bin;

            return trimmed.PadLeft(BillingValidationHelper.BinLength, '0');
        }

        public static Dictionary<string, Guid> BuildInsuranceBinLookup(IEnumerable<Insurance> insurances)
        {
            var lookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var insurance in insurances)
            {
                if (string.IsNullOrWhiteSpace(insurance.BINNumber))
                    continue;

                // Padded the same way as the CSV side so both match regardless of
                // whether the stored BIN kept its leading zeros.
                var bin = PadBin(insurance.BINNumber)!.Trim();
                if (!lookup.ContainsKey(bin))
                    lookup[bin] = insurance.Id;
            }

            return lookup;
        }

        public static ImportErrorItemDto? ResolveInsuranceFromCsvBin(
            int rowNumber,
            BillingDto dto,
            Dictionary<string, Guid> binLookup)
        {
            var bin = dto.PrimaryInsuranceBinNo?.Trim();
            var ndc = dto.NDC;
            var rxNo = dto.RXNO;

            if (string.IsNullOrWhiteSpace(bin))
                return ImportErrorHelper.RowError(
                    rowNumber,
                    "PrimaryInsuranceBinNo",
                    CustomMessage.BillingCsvPrimaryBinRequired,
                    bin,
                    ndc,
                    rxNo);

            if (!BillingValidationHelper.IsValidSixDigitBin(bin))
                return ImportErrorHelper.RowError(
                    rowNumber,
                    "PrimaryInsuranceBinNo",
                    CustomMessage.BillingCsvPrimaryBinInvalidFormat,
                    bin,
                    ndc,
                    rxNo);

            if (!binLookup.TryGetValue(bin, out var insuranceId))
                return ImportErrorHelper.RowError(
                    rowNumber,
                    "PrimaryInsuranceBinNo",
                    CustomMessage.BillingCsvPrimaryBinNotRecognized(bin),
                    bin,
                    ndc,
                    rxNo);

            dto.PrimaryInsuranceBinNo = bin;
            dto.InsuranceId = insuranceId;
            return null;
        }
    }
}
