using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class SetStudentLocationCommandValidator : AbstractValidator<SetStudentLocationCommand>
    {
        public SetStudentLocationCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue).WithMessage("LatitudeInvalid");
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue).WithMessage("LongitudeInvalid");
            RuleFor(x => x).Must(x => x.Latitude.HasValue == x.Longitude.HasValue).WithMessage("LocationIncomplete");
        }
    }
}
