using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ErrorKeys.UserRequired);


            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage(ErrorKeys.RoleRequired);

            RuleFor(x => x.LoginName)
                .NotEmpty().WithMessage(ErrorKeys.LoginNameRequired)
                .MinimumLength(3).WithMessage(ErrorKeys.LoginNameTooShort);
        }
    }
}
