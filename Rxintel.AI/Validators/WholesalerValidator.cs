using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class WholesalerValidator : AbstractValidator<WholesalerDto>
    {
        public WholesalerValidator()
        {
            RuleFor(x => x.WholesalerName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_WHOLESALER_NAME)
                .Must(PharmacyValidationHelper.IsValidWholesalerName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_WHOLESALER_NAME_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_WHOLESALER_NAME);

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_WHOLESALER_ADDRESS)
                .When(x => !string.IsNullOrWhiteSpace(x.Address));

            RuleFor(x => x.Phone)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_WHOLESALER_PHONE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_WHOLESALER_EMAIL_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_WHOLESALER_EMAIL)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
        }
    }
}
