using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateHomeworkCommandValidator : AbstractValidator<UpdateHomeworkCommand>
    {
        public UpdateHomeworkCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.Description).NotEmpty().WithMessage("DescriptionRequired");
        }
    }
}
