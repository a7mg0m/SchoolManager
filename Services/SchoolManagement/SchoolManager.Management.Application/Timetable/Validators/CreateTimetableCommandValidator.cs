using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateTimetableCommandValidator : AbstractValidator<CreateTimetableCommand>
    {
        public CreateTimetableCommandValidator()
        {
            RuleFor(x => x.StageId).NotEmpty().WithMessage("StageIdRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.Slots).NotNull().WithMessage("SlotsRequired");
        }
    }
}
