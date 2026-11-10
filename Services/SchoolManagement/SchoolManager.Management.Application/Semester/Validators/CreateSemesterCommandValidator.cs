using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateSemesterCommandValidator : AbstractValidator<CreateSemesterCommand>
    {
        public CreateSemesterCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("EndDateMustBeAfterStartDate");
        }
    }
}


