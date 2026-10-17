using FluentValidation;

namespace SchoolManager.Notification.Application
{
    public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.Description).NotEmpty().WithMessage("DescriptionRequired");
            RuleFor(x => x.NotificationType).IsInEnum().WithMessage("NotificationTypeInvalid");
            RuleForEach(x => x.Targets).Must(HaveAnyTarget).WithMessage("TargetRequired");
        }

        private static bool HaveAnyTarget(TargetInputDto t)
            => t.GradeId.HasValue || t.SectionId.HasValue || t.TeacherId.HasValue || t.StudentId.HasValue;
    }
}
