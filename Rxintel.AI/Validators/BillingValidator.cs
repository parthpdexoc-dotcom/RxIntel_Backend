using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class BillingValidator : AbstractValidator<BillingDto>
    {
        public BillingValidator()
        {
            RuleFor(x => x.PharmacyId)
                .Must(BillingValidationHelper.IsNotEmptyGuid)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_PHARMACY_ID);

            RuleFor(x => x.InsuranceId)
                .Must(BillingValidationHelper.IsNotEmptyGuid)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_INSURANCE_ID);

            RuleFor(x => x.NDC)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_BILLING_NDC)
                .Must(BillingValidationHelper.IsValidBillingNdc)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_NDC_FORMAT)
                .MaximumLength(ValidationHelperBase.MaxNdcLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_NDC);

            RuleFor(x => x.QuantityBilled)
                .Must(BillingValidationHelper.IsValidQuantityBilled)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_QUANTITY);

            RuleFor(x => x.RXNO)
                .Must(BillingValidationHelper.IsValidRxNo)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_RXNO_FORMAT)

                .MaximumLength(BillingValidationHelper.MaxRxNoLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_RXNO)
                .When(x => !string.IsNullOrWhiteSpace(x.RXNO));

            RuleFor(x => x.BillDate)
                .Must(BillingValidationHelper.IsValidBillDate)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_BILL_DATE);

            RuleFor(x => x.DrugName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_BILLING_DRUG_NAME)
                .Must(BillingValidationHelper.IsValidDrugName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_DRUG_NAME_FORMAT)
                .MaximumLength(BillingValidationHelper.MaxDrugNameLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_DRUG_NAME);

            RuleFor(x => x.DrugForm)
                .Must(BillingValidationHelper.IsValidDrugForm)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_DRUG_FORM_FORMAT)
                .MaximumLength(BillingValidationHelper.MaxDrugFormLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_DRUG_FORM)
                .When(x => !string.IsNullOrWhiteSpace(x.DrugForm));

            RuleFor(x => x.PackageSize)
                .Must(BillingValidationHelper.IsValidPackageSize)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_PACKAGE_SIZE_FORMAT)
                .MaximumLength(BillingValidationHelper.MaxPackageSizeLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_PACKAGE_SIZE)
                .When(x => !string.IsNullOrWhiteSpace(x.PackageSize));

            RuleFor(x => x.DrugTherapy)
                .Must(BillingValidationHelper.IsValidDrugTherapy)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_DRUG_THERAPY_FORMAT)
                .MaximumLength(BillingValidationHelper.MaxDrugTherapyLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_DRUG_THERAPY)
                .When(x => !string.IsNullOrWhiteSpace(x.DrugTherapy));

            RuleFor(x => x.Days)
                .Must(BillingValidationHelper.IsValidDays)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_DAYS)
                .When(x => x.Days.HasValue);

            RuleFor(x => x.PrimaryInsurancePaidAmount)
                .Must(BillingValidationHelper.IsValidPaidAmount)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_PRIMARY_PAID_AMOUNT);

            RuleFor(x => x.PrimaryInsuranceBinNo)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_BILLING_PRIMARY_BIN)
                .Must(BillingValidationHelper.IsValidSixDigitBin)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_PRIMARY_BIN_FORMAT)
                .MaximumLength(BillingValidationHelper.BinLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_PRIMARY_BIN);

            RuleFor(x => x.SecondaryInsurancePaidAmount)
                .Must(value => !value.HasValue || BillingValidationHelper.IsValidPaidAmount(value.Value))
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_SECONDARY_PAID_AMOUNT)
                .When(x => x.SecondaryInsurancePaidAmount.HasValue);

            RuleFor(x => x.SecondaryInsuranceId)
                .Must(id => !id.HasValue || BillingValidationHelper.IsNotEmptyGuid(id.Value))
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_SECONDARY_INSURANCE_ID)
                .When(x => x.SecondaryInsuranceId.HasValue);

            RuleFor(x => x.SecondaryInsuranceId)
                .Must(id => id.HasValue && BillingValidationHelper.IsNotEmptyGuid(id.Value))
                    .WithMessage(ModelValidationErrorCodes.REQUIRED_BILLING_SECONDARY_INSURANCE_ID)
                .When(x => x.SecondaryInsurancePaidAmount.GetValueOrDefault() > 0
                           && string.IsNullOrWhiteSpace(x.SecondaryInsurance));

            RuleFor(x => x.SecondaryInsurance)
                .Must(BillingValidationHelper.IsValidInsuranceProviderName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_SECONDARY_INSURANCE_FORMAT)
                .MaximumLength(BillingValidationHelper.MaxInsuranceProviderLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_SECONDARY_INSURANCE)
                .When(x => !string.IsNullOrWhiteSpace(x.SecondaryInsurance)
                           && !x.SecondaryInsuranceId.HasValue);

            RuleFor(x => x.SecondaryInsurance)
                .NotEmpty()
                    .WithMessage(ModelValidationErrorCodes.REQUIRED_BILLING_SECONDARY_INSURANCE)
                .When(x => x.SecondaryInsurancePaidAmount.GetValueOrDefault() > 0
                           && !x.SecondaryInsuranceId.HasValue);

            RuleFor(x => x.SecondaryInsuranceBinNo)
                .Must(BillingValidationHelper.IsValidSixDigitBin)
                    .WithMessage(ModelValidationErrorCodes.INVALID_BILLING_SECONDARY_BIN_FORMAT)
                .MaximumLength(BillingValidationHelper.BinLength)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_BILLING_SECONDARY_BIN)
                .When(x => !string.IsNullOrWhiteSpace(x.SecondaryInsuranceBinNo));
        }
    }
}
