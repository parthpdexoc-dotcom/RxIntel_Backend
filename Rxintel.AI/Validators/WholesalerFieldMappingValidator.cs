using FluentValidation;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.AI.Validators
{
    public class WholesalerFieldMappingValidator : AbstractValidator<WholesalerFieldMappingDto>
    {
        public WholesalerFieldMappingValidator()
        {
            RuleFor(x => x.WholesalerId)
                .NotEmpty()
                .WithMessage("WholesalerId is required");

            RuleFor(x => x.DestinationColumnName)
                .NotEmpty()
                .WithMessage("Destination Column Name is required")
                .MaximumLength(100)
                .WithMessage("Destination Column Name cannot exceed 100 characters");

            RuleFor(x => x.SourceColumnName)
                .MaximumLength(200)
                .WithMessage("Source Column Name cannot exceed 200 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.SourceColumnName));
        }
    }
}
