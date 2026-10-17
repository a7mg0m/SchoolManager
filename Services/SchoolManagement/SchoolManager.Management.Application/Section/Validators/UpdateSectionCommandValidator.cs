using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateSectionCommandValidator : AbstractValidator<UpdateSectionCommand>
    {
        public UpdateSectionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
        }
    }
}
