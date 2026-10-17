using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class DeleteHomeworkCommandValidator : AbstractValidator<DeleteHomeworkCommand>
    {
        public DeleteHomeworkCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
