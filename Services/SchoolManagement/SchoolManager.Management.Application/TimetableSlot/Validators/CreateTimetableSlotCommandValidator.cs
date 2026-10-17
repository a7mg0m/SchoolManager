using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateTimetableSlotCommandValidator : AbstractValidator<CreateTimetableSlotCommand>
    {
        public CreateTimetableSlotCommandValidator()
        {
            RuleFor(x => x.Day).IsInEnum().WithMessage("DayInvalid");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectIdRequired");
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("TeacherIdRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
        }
    }
}
