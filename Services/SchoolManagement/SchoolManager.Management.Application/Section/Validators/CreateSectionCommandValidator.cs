using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateSectionCommandValidator : AbstractValidator<CreateSectionCommand>
    {
        public CreateSectionCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
        }
    }
}
