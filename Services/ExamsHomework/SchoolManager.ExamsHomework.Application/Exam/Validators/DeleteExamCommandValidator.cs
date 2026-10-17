using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteExamCommandValidator : AbstractValidator<DeleteExamCommand>
    {
        public DeleteExamCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
