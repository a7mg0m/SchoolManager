using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkCommandValidator : AbstractValidator<CreateHomeworkCommand>
    {
        public CreateHomeworkCommandValidator()
        {
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectRequired");
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("TeacherRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.Description).NotEmpty().WithMessage("DescriptionRequired");
        }
    }
}
