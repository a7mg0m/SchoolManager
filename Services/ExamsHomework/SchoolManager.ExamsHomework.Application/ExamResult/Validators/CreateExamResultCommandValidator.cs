using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamResultCommandValidator : AbstractValidator<CreateExamResultCommand>
    {
        public CreateExamResultCommandValidator()
        {
            RuleFor(x => x.ExamId).NotEmpty().WithMessage("ExamRequired");
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentRequired");
            RuleFor(x => x.Score).GreaterThanOrEqualTo(0).WithMessage("ScoreInvalid");
        }
    }
}
