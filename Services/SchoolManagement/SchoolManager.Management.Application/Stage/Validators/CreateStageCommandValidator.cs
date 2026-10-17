using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateStageCommandValidator : AbstractValidator<CreateStageCommand>
    {
        public CreateStageCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
        }
    }
}
