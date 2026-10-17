using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateStageCommandValidator : AbstractValidator<UpdateStageCommand>
    {
        public UpdateStageCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
        }
    }
}
