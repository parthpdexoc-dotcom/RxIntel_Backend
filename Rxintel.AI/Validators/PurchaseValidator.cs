using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class PurchaseValidator : AbstractValidator<PurchaseDto>
    {
        public PurchaseValidator()
        {
            RuleFor(x => x.PharmacyId)
                .Must(PurchaseValidationHelper.IsNotEmptyGuid)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_PHARMACY_ID);

            RuleFor(x => x.WholesalerId)
                .Must(PurchaseValidationHelper.IsNotEmptyGuid)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_WHOLESALER_ID);

            RuleFor(x => x.NDC)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PURCHASE_NDC)
                .Must(PurchaseValidationHelper.IsValidPurchaseNdc)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_NDC_FORMAT)
                .MaximumLength(ValidationHelperBase.MaxNdcLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PURCHASE_NDC);

            RuleFor(x => x.QuantityPurchased)
                .Must(PurchaseValidationHelper.IsValidQuantityPurchased)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_QUANTITY);

            RuleFor(x => x.PackageSize)
                .Must(PurchaseValidationHelper.IsValidPackageSize)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_PACKAGE_SIZE_FORMAT)
                .MaximumLength(PurchaseValidationHelper.MaxPackageSizeLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PURCHASE_PACKAGE_SIZE)
                .When(x => !string.IsNullOrWhiteSpace(x.PackageSize));

            RuleFor(x => x.InvoiceDate)
                .Must(PurchaseValidationHelper.IsValidInvoiceDate)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_INVOICE_DATE)
                .When(x => x.InvoiceDate.HasValue);

            RuleFor(x => x.InvoiceNumber)
                .Must(PurchaseValidationHelper.IsValidInvoiceNumber)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PURCHASE_INVOICE_NUMBER_FORMAT)
                .MaximumLength(PurchaseValidationHelper.MaxInvoiceNumberLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PURCHASE_INVOICE_NUMBER)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceNumber));
        }
    }
}
