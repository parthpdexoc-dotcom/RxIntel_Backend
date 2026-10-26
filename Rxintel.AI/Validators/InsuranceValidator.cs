using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class InsuranceValidator : AbstractValidator<InsuranceDto>
    {
        public InsuranceValidator()
        {
            RuleFor(x => x.InsuranceName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_INSURANCE_NAME)
                .Must(PharmacyValidationHelper.IsValidInsuranceName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_INSURANCE_NAME_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_INSURANCE_NAME);

            RuleFor(x => x.BINNumber)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_INSURANCE_BIN_NUMBER)
                .Must(PharmacyValidationHelper.IsValidBinNumber)
                    .WithMessage(ModelValidationErrorCodes.INVALID_INSURANCE_BIN_NUMBER_FORMAT);

            RuleFor(x => x.Phone)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_INSURANCE_PHONE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_INSURANCE_EMAIL_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_INSURANCE_EMAIL)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
        }
    }
}
