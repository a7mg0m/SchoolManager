using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateHomeworkQuestionCommandValidator : AbstractValidator<UpdateHomeworkQuestionCommand>
    {
        public UpdateHomeworkQuestionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.QuestionHeader).NotEmpty().WithMessage("QuestionHeaderRequired");
        }
    }
}
