using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("FullNameArRequired");
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage("FullNameEnRequired");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumberRequired");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("EmailInvalid");
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue).WithMessage("LatitudeInvalid");
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue).WithMessage("LongitudeInvalid");
            RuleFor(x => x).Must(x => x.Latitude.HasValue == x.Longitude.HasValue).WithMessage("LocationIncomplete");
        }
    }
}
