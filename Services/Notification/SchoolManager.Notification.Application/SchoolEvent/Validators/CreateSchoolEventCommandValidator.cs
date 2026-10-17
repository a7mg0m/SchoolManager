using FluentValidation;

namespace SchoolManager.Notification.Application
{
    public class CreateSchoolEventCommandValidator : AbstractValidator<CreateSchoolEventCommand>
    {
        public CreateSchoolEventCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.Description).NotEmpty().WithMessage("DescriptionRequired");
            RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("EndTimeMustBeAfterStartTime");
            RuleFor(x => x.EventFor).IsInEnum().WithMessage("EventForInvalid");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearRequired");
            RuleFor(x => x.SemesterId).NotEmpty().WithMessage("SemesterRequired");
            RuleForEach(x => x.Targets).Must(HaveAnyTarget).WithMessage("TargetRequired");
        }

        private static bool HaveAnyTarget(TargetInputDto t)
            => t.GradeId.HasValue || t.SectionId.HasValue || t.TeacherId.HasValue || t.StudentId.HasValue;
    }
}
