using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateTimetableCommandValidator : AbstractValidator<CreateTimetableCommand>
    {
        public CreateTimetableCommandValidator()
        {
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
        }
    }
}
