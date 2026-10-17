using FluentValidation;

namespace SchoolManager.Notification.Application
{
    public class DeleteSchoolEventCommandValidator : AbstractValidator<DeleteSchoolEventCommand>
    {
        public DeleteSchoolEventCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
