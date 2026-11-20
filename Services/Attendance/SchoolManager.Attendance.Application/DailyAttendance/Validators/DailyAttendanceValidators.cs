using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class RecordPeriodAttendanceCommandValidator : AbstractValidator<RecordPeriodAttendanceCommand>
    {
        public RecordPeriodAttendanceCommandValidator()
        {
            RuleFor(x => x.TimetableSlotId).NotEmpty().WithMessage("TimetableSlotIdRequired");
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.Date).NotEmpty().WithMessage("DateRequired");
            RuleFor(x => x.Students).NotEmpty().WithMessage("StudentsRequired");
            RuleForEach(x => x.Students).ChildRules(student =>
            {
                student.RuleFor(s => s.StudentId).NotEmpty().WithMessage("StudentIdRequired");
                student.RuleFor(s => s.Status).IsInEnum().WithMessage("StatusInvalid");
                student.RuleFor(s => s.AbsenceReason).MaximumLength(Domain.PeriodAttendance.MaxAbsenceReasonLength).WithMessage("AbsenceReasonTooLong");
            });
        }
    }

    public class SetDailyAttendanceCommandValidator : AbstractValidator<SetDailyAttendanceCommand>
    {
        public SetDailyAttendanceCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.Date).NotEmpty().WithMessage("DateRequired");
            RuleFor(x => x.Status).IsInEnum().WithMessage("StatusInvalid");
            RuleFor(x => x.Notes).MaximumLength(Domain.DailyAttendance.MaxNotesLength).WithMessage("NotesTooLong");
        }
    }

    public class CheckOutStudentCommandValidator : AbstractValidator<CheckOutStudentCommand>
    {
        public CheckOutStudentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }

    public class DeleteDailyAttendanceCommandValidator : AbstractValidator<DeleteDailyAttendanceCommand>
    {
        public DeleteDailyAttendanceCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
