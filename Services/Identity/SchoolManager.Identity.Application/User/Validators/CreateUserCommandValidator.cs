using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
          
            RuleFor(x => x.LoginName)
                .NotEmpty().WithMessage(ErrorKeys.LoginNameRequired)
                .MinimumLength(3).WithMessage(ErrorKeys.LoginNameTooShort);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorKeys.EmailRequired)
                .EmailAddress().WithMessage(ErrorKeys.EmailInvalid);

            RuleFor(x => x.HashPassword)
                .NotEmpty().WithMessage(ErrorKeys.PasswordRequired);

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage(ErrorKeys.RoleRequired);
        }
    }
}
