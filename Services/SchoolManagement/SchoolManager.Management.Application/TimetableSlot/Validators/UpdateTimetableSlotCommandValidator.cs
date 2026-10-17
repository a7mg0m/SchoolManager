using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateTimetableSlotCommandValidator : AbstractValidator<UpdateTimetableSlotCommand>
    {
        public UpdateTimetableSlotCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Day).IsInEnum().WithMessage("DayInvalid");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectIdRequired");
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("TeacherIdRequired");
        }
    }
}
