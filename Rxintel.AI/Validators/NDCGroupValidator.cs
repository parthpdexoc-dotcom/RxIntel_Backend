using FluentValidation;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;

namespace Rxintel.AI.Validators
{
    public class NDCGroupValidator : AbstractValidator<NDCGroupDto>
    {
        public NDCGroupValidator()
        {
            RuleFor(x => x.ParentMedicineId)
                .NotEmpty()
                .WithMessage(CustomMessage.ParentRequiredMessage("ParentMedicineId"));
        }
    }
}
