using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteHomeworkQuestionCommandValidator : AbstractValidator<DeleteHomeworkQuestionCommand>
    {
        public DeleteHomeworkQuestionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
