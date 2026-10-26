using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Shared FluentValidation → import-row error mapping for CSV bulk upsert.
    /// </summary>
    public static class ImportRowValidationHelper
    {
        public static IEnumerable<ImportErrorItemDto> CollectValidationErrors<TDto>(
            IValidator<TDto> validator,
            int rowNumber,
            TDto row,
            Func<TDto, string?> ndcSelector,
            Func<TDto, string?> secondaryKeySelector)
        {
            var result = validator.Validate(row);

            foreach (var failure in result.Errors)
            {
                var message = ModelValidationErrorCodes.GetErrorMessage(failure.ErrorMessage);
                if (string.IsNullOrWhiteSpace(message))
                    message = failure.ErrorMessage;

                var fieldName = string.IsNullOrWhiteSpace(failure.PropertyName)
                    ? "Row"
                    : failure.PropertyName;

                yield return ImportErrorHelper.RowError(
                    rowNumber,
                    fieldName,
                    message,
                    ImportErrorHelper.GetFieldValue(row, fieldName),
                    ndcSelector(row),
                    secondaryKeySelector(row));
            }
        }

        /// <summary>
        /// Unhyphenated 8-10 digit NDCs that FDA did not resolve must fail with the
        /// existing NDC format error (10-digit values would otherwise pass validation).
        /// </summary>
        public static void AppendUnresolvedShortNdcError<TDto>(
            List<ImportErrorItemDto> validationErrors,
            int rowNumber,
            TDto row,
            Func<TDto, string?> ndcSelector,
            Func<TDto, string?> secondaryKeySelector,
            string ndcFormatErrorCode)
        {
            var ndc = ndcSelector(row);
            if (ValidationHelperBase.IsElevenDigitNdc(ndc))
                return;

            if (validationErrors.Any(error =>
                    string.Equals(error.FieldName, nameof(BillingDto.NDC), StringComparison.OrdinalIgnoreCase)))
                return;

            validationErrors.Add(ImportErrorHelper.RowError(
                rowNumber,
                nameof(BillingDto.NDC),
                ModelValidationErrorCodes.GetErrorMessage(ndcFormatErrorCode),
                ndc,
                ndc,
                secondaryKeySelector(row)));
        }
    }
}
