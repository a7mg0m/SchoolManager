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
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue).WithMessage("LatitudeInvalid");
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue).WithMessage("LongitudeInvalid");
            RuleFor(x => x).Must(x => x.Latitude.HasValue == x.Longitude.HasValue).WithMessage("LocationIncomplete");
        }
    }
}
