using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class PharmacySoftwareValidator : AbstractValidator<PharmacySoftwareDto>
    {
        public PharmacySoftwareValidator()
        {
            RuleFor(x => x.SoftwareName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_SOFTWARE_NAME)
                .Must(PharmacyValidationHelper.IsValidSoftwareName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_SOFTWARE_NAME_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_SOFTWARE_NAME);
        }
    }
}
