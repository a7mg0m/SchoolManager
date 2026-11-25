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
            RuleFor(x => x.SemesterId).NotEmpty().WithMessage("SemesterIdRequired");
            RuleFor(x => x.Slots).NotNull().WithMessage("SlotsRequired");

            // Week settings
            RuleForEach(x => x.WorkDays).SetValidator(new TimetableWorkDayDtoValidator());
            RuleFor(x => x.WorkDays).Must(TimetableWorkDayDtoValidator.DaysUnique).WithMessage("WorkDayDuplicated");
            RuleFor(x => x).Must(x => TimetableWorkDayDtoValidator.StartTimeGiven(x.DayStartTime, x.WorkDays))
                .WithMessage("DayStartTimeRequired");
        }
    }
}
