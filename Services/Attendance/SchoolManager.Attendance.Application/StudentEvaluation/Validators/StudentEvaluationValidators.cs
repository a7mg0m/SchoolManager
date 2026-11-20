using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class EvaluateStudentCommandValidator : AbstractValidator<EvaluateStudentCommand>
    {
        public EvaluateStudentCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentIdRequired");
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectIdRequired");
            RuleFor(x => x.TimetableSlotId).NotEmpty().WithMessage("TimetableSlotIdRequired");
            RuleFor(x => x.Date).NotEmpty().WithMessage("DateRequired");
            RuleFor(x => x.Focus).IsInEnum().WithMessage("FocusInvalid");
            RuleFor(x => x.Interaction).IsInEnum().WithMessage("InteractionInvalid");
            RuleFor(x => x.Behavior).IsInEnum().WithMessage("BehaviorInvalid");
            RuleFor(x => x.HealthStatus).IsInEnum().WithMessage("HealthStatusInvalid");
            RuleFor(x => x.Comment).MaximumLength(Domain.StudentEvaluation.MaxCommentLength).WithMessage("CommentTooLong");
        }
    }

    public class UpdateStudentEvaluationCommandValidator : AbstractValidator<UpdateStudentEvaluationCommand>
    {
        public UpdateStudentEvaluationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Focus).IsInEnum().WithMessage("FocusInvalid");
            RuleFor(x => x.Interaction).IsInEnum().WithMessage("InteractionInvalid");
            RuleFor(x => x.Behavior).IsInEnum().WithMessage("BehaviorInvalid");
            RuleFor(x => x.HealthStatus).IsInEnum().WithMessage("HealthStatusInvalid");
            RuleFor(x => x.Comment).MaximumLength(Domain.StudentEvaluation.MaxCommentLength).WithMessage("CommentTooLong");
        }
    }

    public class DeleteStudentEvaluationCommandValidator : AbstractValidator<DeleteStudentEvaluationCommand>
    {
        public DeleteStudentEvaluationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
