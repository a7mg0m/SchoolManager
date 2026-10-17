using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteHomeworkPublishCommandValidator : AbstractValidator<DeleteHomeworkPublishCommand>
    {
        public DeleteHomeworkPublishCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
