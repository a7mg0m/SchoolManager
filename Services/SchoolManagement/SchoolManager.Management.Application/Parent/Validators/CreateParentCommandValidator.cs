using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateParentCommandValidator : AbstractValidator<CreateParentCommand>
    {
        public CreateParentCommandValidator()
        {
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("FullNameArRequired");
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage("FullNameEnRequired");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumberRequired");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("EmailInvalid");
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");

            // Each student's home location: both values or neither, in range
            RuleForEach(x => x.Students).ChildRules(student =>
            {
                student.RuleFor(s => s.Latitude).InclusiveBetween(-90, 90).When(s => s.Latitude.HasValue).WithMessage("LatitudeInvalid");
                student.RuleFor(s => s.Longitude).InclusiveBetween(-180, 180).When(s => s.Longitude.HasValue).WithMessage("LongitudeInvalid");
                student.RuleFor(s => s).Must(s => s.Latitude.HasValue == s.Longitude.HasValue).WithMessage("LocationIncomplete");
            });
        }
    }
}
