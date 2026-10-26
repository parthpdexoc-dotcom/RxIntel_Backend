using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;

namespace Rxintel.AI.Validators
{
    public class PharmacyUserValidator : AbstractValidator<PharmacyUserDto>
    {
        public PharmacyUserValidator()
        {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_USER_ROLE)
                .When(x => x.Role.HasValue);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_USER_NAME)
                .Must(PharmacyValidationHelper.IsLettersAndSpacesOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_USER_NAME_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_USER_NAME);

            RuleFor(x => x.UserEmail)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_USER_EMAIL)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_USER_EMAIL_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_USER_EMAIL);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_USER_PASSWORD)
                .When(x => x.Id == Guid.Empty);

            RuleFor(x => x.Password)
                .Must(PharmacyValidationHelper.IsValidPharmacyPassword)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_USER_PASSWORD_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_USER_PASSWORD)
                .When(x => x.Id == Guid.Empty);

            RuleFor(x => x)
                .Must(x => string.Equals(x.Password, x.ConfirmPassword, StringComparison.Ordinal))
                .WithMessage(CustomMessage.PasswordMismatch)
                .When(x => x.Id == Guid.Empty);

            RuleFor(x => x.ProfileImage)
                .Must(ProfileImageValidationHelper.IsValidProfileImageFilename)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_USER_PROFILE_IMAGE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.ProfileImage));

            RuleFor(x => x.PrimaryContactName)
                .Must(PharmacyValidationHelper.IsValidPersonName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_CONTACT_NAME_FORMAT)
                .MaximumLength(150)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_NAME)
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryContactName));

            RuleFor(x => x.PrimaryContactTitle)
                 .Must(PharmacyValidationHelper.IsValidContactTitle)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_CONTACT_TITLE_FORMAT)
                 .MaximumLength(100)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_PRIMARY_CONTACT_TITLE)
                 .When(x => !string.IsNullOrWhiteSpace(x.PrimaryContactTitle));

            RuleFor(x => x.PrimaryContactPhone)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_CONTACT_PHONE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryContactPhone));

            RuleFor(x => x.PrimaryWholesalerName)
                .Must(PharmacyValidationHelper.IsValidWholesalerName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_WHOLESALER_NAME_FORMAT)
                .MaximumLength(200)
                    .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_NAME)
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryWholesalerName));

            RuleFor(x => x.PrimaryWholesalerContactName)
                 .Must(PharmacyValidationHelper.IsValidPersonName)
                     .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME_FORMAT)
                 .MaximumLength(150)
                     .WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_CONTACT_NAME)
                 .When(x => !string.IsNullOrWhiteSpace(x.PrimaryWholesalerContactName));

            RuleFor(x => x.PrimaryWholesalerPhone)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_WHOLESALER_PHONE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryWholesalerPhone));

            RuleFor(x => x.PrimaryWholesalerEmail)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PRIMARY_WHOLESALER_EMAIL_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_PRIMARY_WHOLESALER_EMAIL)
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryWholesalerEmail));
        }
    }
}
