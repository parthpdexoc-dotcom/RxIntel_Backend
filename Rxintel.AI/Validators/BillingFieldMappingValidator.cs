using FluentValidation;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.AI.Validators
{
    public class BillingFieldMappingValidator : AbstractValidator<BillingFieldMappingDto>
    {
        public BillingFieldMappingValidator()
        {
            RuleFor(x => x.PharmacySoftwareId)
                .NotEmpty()
                .WithMessage("PharmacySoftwareId is required");

            RuleFor(x => x.DestinationFieldName)
                .NotEmpty()
                .WithMessage("Destination Field Name is required")
                .MaximumLength(100)
                .WithMessage("Destination Field Name cannot exceed 100 characters");

            RuleFor(x => x.SourceFieldName)
                .MaximumLength(200)
                .WithMessage("Source Field Name cannot exceed 200 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.SourceFieldName));
        }
    }
}
