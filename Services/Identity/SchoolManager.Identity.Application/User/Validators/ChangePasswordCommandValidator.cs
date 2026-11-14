using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public const int MinPasswordLength = 6;

        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(ErrorKeys.CurrentPasswordRequired);
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(ErrorKeys.PasswordRequired)
                .MinimumLength(MinPasswordLength).WithMessage(ErrorKeys.PasswordTooShort);
        }
    }
}
