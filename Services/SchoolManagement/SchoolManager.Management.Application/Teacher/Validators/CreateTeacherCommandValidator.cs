using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
    {
        public CreateTeacherCommandValidator()
        {
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("FullNameArRequired");
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage("FullNameEnRequired");
            RuleFor(x => x.NationalId).NotEmpty().WithMessage("NationalIdRequired");
            RuleFor(x => x.Gender).NotEmpty().WithMessage("GenderRequired");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumberRequired");
            // Required: the teacher's login User in the Identity service needs a unique email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EmailRequired")
                .EmailAddress().WithMessage("EmailInvalid")
                .MaximumLength(256);
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
        }
    }
}
