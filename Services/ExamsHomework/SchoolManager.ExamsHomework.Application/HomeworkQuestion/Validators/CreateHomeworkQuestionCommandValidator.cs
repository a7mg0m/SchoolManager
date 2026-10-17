using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkQuestionCommandValidator : AbstractValidator<CreateHomeworkQuestionCommand>
    {
        public CreateHomeworkQuestionCommandValidator()
        {
            RuleFor(x => x.HomeworkId).NotEmpty().WithMessage("HomeworkRequired");
            RuleFor(x => x.QuestionHeader).NotEmpty().WithMessage("QuestionHeaderRequired");
        }
    }
}
