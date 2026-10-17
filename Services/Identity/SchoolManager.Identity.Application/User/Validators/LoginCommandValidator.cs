using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.LoginName)
                .NotEmpty().WithMessage(ErrorKeys.LoginNameRequired);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ErrorKeys.PasswordRequired);
        }
    }
}
