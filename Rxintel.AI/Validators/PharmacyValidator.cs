using FluentValidation;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Models.Constants;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.AI.Validators
{
    public class PharmacyValidator : AbstractValidator<PharmacyDto>
    {
        public PharmacyValidator(IStateRepository stateRepository)
        {
            RuleFor(x => x.DEA)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_DEA)
                .Must(PharmacyValidationHelper.HasValidDeaFormat)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_DEA_FORMAT)
                .Must((dto, dea) => PharmacyValidationHelper.HasValidDeaSecondLetter(dea, dto.BusinessTradeName))
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_DEA_BUSINESS_NAME_LETTER)
                .When(x => PharmacyValidationHelper.HasValidDeaFormat(x.DEA) &&
                           !string.IsNullOrWhiteSpace(x.BusinessTradeName))
                .Must(PharmacyValidationHelper.HasValidDeaChecksum)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_DEA_CHECKSUM)
                .When(x => PharmacyValidationHelper.HasValidDeaFormat(x.DEA));

            RuleFor(x => x.BusinessLegalName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_BUSINESS_LEGAL_NAME)
                .Must(PharmacyValidationHelper.IsValidBusinessName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_BUSINESS_LEGAL_NAME_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_BUSINESS_LEGAL_NAME);

            RuleFor(x => x.BusinessTradeName)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_NAME)
                .Must(PharmacyValidationHelper.IsValidBusinessName)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_NAME_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_BUSINESS_TRADE_NAME);

            RuleFor(x => x.DeaExpirationDate)
                .NotNull().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_DEA_EXPIRATION_DATE);

            RuleFor(x => x.StateBoardOfPharmacyLicenseNumber)
                .CustomAsync(async (license, context, _) =>
                {
                    if (string.IsNullOrWhiteSpace(license))
                    {
                        return;
                    }

                    var dto = context.InstanceToValidate;
                    var ruleId = await ResolveStateBoardLicenseRuleIdAsync(
                        stateRepository,
                        dto.StateId);

                    if (!PharmacyValidationHelper.IsValidStateBoardLicenseNumber(license, ruleId))
                    {
                        context.AddFailure(
                            PharmacyValidationHelper.GetStateBoardLicenseValidationMessage(ruleId));
                    }
                });

            RuleFor(x => x.BopLicenseExpirationDate)
                .NotNull().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_BOP_LICENSE_EXPIRATION_DATE);

            RuleFor(x => x.GlobalLocationNumber)
                .Must(PharmacyValidationHelper.IsThirteenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_GLN_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.GlobalLocationNumber));

            RuleFor(x => x.PharmacyAddress)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_ADDRESS)
                .Must(PharmacyValidationHelper.IsValidAddress)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_ADDRESS_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_ADDRESS);

            RuleFor(x => x.StreetAddress)
                .Must(PharmacyValidationHelper.IsValidAddress)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_STREET_ADDRESS_FORMAT)
                .MaximumLength(200).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_STREET_ADDRESS)
                .When(x => !string.IsNullOrWhiteSpace(x.StreetAddress));

            RuleFor(x => x.City)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_CITY)
                .Must(PharmacyValidationHelper.IsValidCity)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_CITY_FORMAT)
                .MaximumLength(100).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_CITY);

            RuleFor(x => x.StateId)
                .NotNull().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_STATE)
                .NotEqual(Guid.Empty).WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_STATE);

            RuleFor(x => x.NPI)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_NPI_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.NPI));

            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_ZIP)
                .Must(PharmacyValidationHelper.IsValidZip)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_ZIP_FORMAT);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_PHONE)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_PHONE_FORMAT);

            RuleFor(x => x.Fax)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_FAX_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.Fax));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ModelValidationErrorCodes.REQUIRED_PHARMACY_EMAIL)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_EMAIL_FORMAT)
                .MaximumLength(150).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_EMAIL);

            RuleFor(x => x.PharmacyBillingSoftwareName)
                .Must(PharmacyValidationHelper.IsValidSoftwareSystem)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_SOFTWARE_SYSTEM_FORMAT)
                .MaximumLength(100).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_SOFTWARE_SYSTEM)
                .When(x => !string.IsNullOrWhiteSpace(x.PharmacyBillingSoftwareName));

            RuleFor(x => x.PharmacyBillingSoftwarePhone)
                .Must(PharmacyValidationHelper.IsTenDigitsOnly)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_SOFTWARE_PHONE_FORMAT)
                .When(x => !string.IsNullOrWhiteSpace(x.PharmacyBillingSoftwarePhone));

            RuleFor(x => x.PharmacyBillingSoftwareEmail)
                .Must(PharmacyValidationHelper.IsValidPharmacyEmail)
                    .WithMessage(ModelValidationErrorCodes.INVALID_PHARMACY_SOFTWARE_EMAIL_FORMAT)
                .MaximumLength(255).WithMessage(ModelValidationErrorCodes.MAXIMUM_LENGTH_PHARMACY_SOFTWARE_EMAIL)
                .When(x => !string.IsNullOrWhiteSpace(x.PharmacyBillingSoftwareEmail));
        }

        private static async Task<string?> ResolveStateBoardLicenseRuleIdAsync(
            IStateRepository stateRepository,
            Guid? stateId)
        {
            if (stateId == null || stateId == Guid.Empty)
            {
                return null;
            }

            var state = await stateRepository.GetByIdAsync(stateId.Value, nameof(State.StateId));
            return state?.StateBoardLicenseRuleId;
        }
    }
}
