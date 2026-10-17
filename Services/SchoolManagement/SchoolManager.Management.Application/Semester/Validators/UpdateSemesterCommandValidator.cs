using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateSemesterCommandValidator : AbstractValidator<UpdateSemesterCommand>
    {
        public UpdateSemesterCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
        }
    }
}
