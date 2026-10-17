using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkPublishCommandValidator : AbstractValidator<CreateHomeworkPublishCommand>
    {
        public CreateHomeworkPublishCommandValidator()
        {
            RuleFor(x => x.HomeworkId).NotEmpty().WithMessage("HomeworkRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionRequired");
            RuleFor(x => x.Status).IsInEnum().WithMessage("StatusInvalid");
        }
    }
}
