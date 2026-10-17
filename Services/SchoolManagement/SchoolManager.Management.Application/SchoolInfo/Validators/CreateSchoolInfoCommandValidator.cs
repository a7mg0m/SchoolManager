using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateSchoolInfoCommandValidator : AbstractValidator<CreateSchoolInfoCommand>
    {
        public CreateSchoolInfoCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("EmailInvalid");
        }
    }
}
