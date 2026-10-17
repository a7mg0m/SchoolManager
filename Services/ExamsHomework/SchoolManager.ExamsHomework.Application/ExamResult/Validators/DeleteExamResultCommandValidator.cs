using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteExamResultCommandValidator : AbstractValidator<DeleteExamResultCommand>
    {
        public DeleteExamResultCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
