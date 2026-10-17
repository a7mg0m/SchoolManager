using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage(ErrorKeys.UserRequired);
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(ErrorKeys.FirstNameRequired);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(ErrorKeys.LastNameRequired);
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorKeys.EmailRequired)
                .EmailAddress().WithMessage(ErrorKeys.EmailInvalid);
        }
    }
}
