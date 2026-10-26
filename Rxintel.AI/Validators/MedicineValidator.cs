using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class MedicineValidator : AbstractValidator<MedicineDto>
    {
        public MedicineValidator()
        {
            RuleFor(x => x.NDC)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_MEDICINE_NDC)
                .Must(PharmacyValidationHelper.IsValidNdc)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_NDC_FORMAT)
                .MaximumLength(100).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_NDC);

            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_MEDICINE_DRUG_NAME)
                .Must(PharmacyValidationHelper.IsValidDrugName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_DRUG_NAME_FORMAT)
                .MaximumLength(250).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_DRUG_NAME);

            RuleFor(x => x.Strength)
                .Must(MedicineValidationHelper.IsValidStrength)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_STRENGTH_VALUE_FORMAT)
                .MaximumLength(100).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_STRENGTH)
                .When(x => !string.IsNullOrWhiteSpace(x.Strength));

            RuleFor(x => x.StrengthUnit)
                .Must(value => MedicineValidationHelper.IsValidCommaSeparatedEnumLabels<UnitStrength>(value))
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_STRENGTH)
                .MaximumLength(500).WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_STRENGTH)
                .When(x => !string.IsNullOrWhiteSpace(x.StrengthUnit));

            RuleFor(x => x.PackageSize)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_MEDICINE_PACKAGE_SIZE)
                .Must(MedicineValidationHelper.IsValidPackageSize)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_PACKAGE_SIZE_FORMAT)
                .MaximumLength(50).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_PACKAGE_SIZE);

            RuleFor(x => x.DosageForm)
                .Must(MedicineValidationHelper.IsValidMedicineDosageForm)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_FORM)
                .MaximumLength(250).WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_FORM)
                .When(x => !string.IsNullOrWhiteSpace(x.DosageForm));

            RuleFor(x => x.ProductNDC)
                .MaximumLength(20).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_PRODUCT_NDC)
                .When(x => !string.IsNullOrWhiteSpace(x.ProductNDC));

            RuleFor(x => x.Generic)
                .Must(MedicineValidationHelper.IsValidGeneric)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_GENERIC_FORMAT)
                .MaximumLength(1000).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_GENERIC)
                .When(x => !string.IsNullOrWhiteSpace(x.Generic));

            RuleFor(x => x.Route)
                .Must(MedicineValidationHelper.IsValidRoute)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_ROUTE_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_ROUTE)
                .When(x => !string.IsNullOrWhiteSpace(x.Route));

            RuleFor(x => x.Labeler)
                .Must(MedicineValidationHelper.IsValidLabeler)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_LABELER_FORMAT)
                .MaximumLength(250).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_LABELER)
                .When(x => !string.IsNullOrWhiteSpace(x.Labeler));

            RuleFor(x => x.Suffix)
                .Must(MedicineValidationHelper.IsValidSuffix)
                    .WithMessage(ModelValidationErrorCodes.INVALID_MEDICINE_SUFFIX_FORMAT)
                .MaximumLength(500).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_MEDICINE_SUFFIX)
                .When(x => !string.IsNullOrWhiteSpace(x.Suffix));
        }
    }
}
