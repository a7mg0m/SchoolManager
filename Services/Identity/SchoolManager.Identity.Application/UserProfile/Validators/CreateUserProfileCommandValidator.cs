using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
    {
        public CreateUserProfileCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage(ErrorKeys.UserRequired);
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(ErrorKeys.FirstNameRequired);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(ErrorKeys.LastNameRequired);
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorKeys.EmailRequired)
                .EmailAddress().WithMessage(ErrorKeys.EmailInvalid);
        }
    }
}
