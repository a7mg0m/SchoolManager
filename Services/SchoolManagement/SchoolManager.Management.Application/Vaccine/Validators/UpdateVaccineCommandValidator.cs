using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateVaccineCommandValidator : AbstractValidator<UpdateVaccineCommand>
    {
        public UpdateVaccineCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.TotalDoses).GreaterThan(0).WithMessage("TotalDosesInvalid");
        }
    }
}
