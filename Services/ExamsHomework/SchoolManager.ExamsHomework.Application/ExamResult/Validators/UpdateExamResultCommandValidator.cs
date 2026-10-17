using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateExamResultCommandValidator : AbstractValidator<UpdateExamResultCommand>
    {
        public UpdateExamResultCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentRequired");
            RuleFor(x => x.Score).GreaterThanOrEqualTo(0).WithMessage("ScoreInvalid");
        }
    }
}
