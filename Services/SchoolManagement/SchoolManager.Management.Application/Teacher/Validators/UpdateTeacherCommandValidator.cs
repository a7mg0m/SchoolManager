using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
    {
        public UpdateTeacherCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("FullNameArRequired");
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage("FullNameEnRequired");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumberRequired");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("EmailInvalid");
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
        }
    }
}
