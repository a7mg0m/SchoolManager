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
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("EmailInvalid");
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
        }
    }
}
