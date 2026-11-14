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
            // Required: the teacher's login User in the Identity service needs a unique email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EmailRequired")
                .EmailAddress().WithMessage("EmailInvalid")
                .MaximumLength(256);
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
        }
    }
}
