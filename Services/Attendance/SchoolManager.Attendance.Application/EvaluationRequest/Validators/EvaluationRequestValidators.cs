using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class CreateEvaluationRequestCommandValidator : AbstractValidator<CreateEvaluationRequestCommand>
    {
        public CreateEvaluationRequestCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentIdRequired");
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("TeacherIdRequired");
            RuleFor(x => x.Note).MaximumLength(Domain.EvaluationRequest.MaxNoteLength).WithMessage("NoteTooLong");
        }
    }

    public class CancelEvaluationRequestCommandValidator : AbstractValidator<CancelEvaluationRequestCommand>
    {
        public CancelEvaluationRequestCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
