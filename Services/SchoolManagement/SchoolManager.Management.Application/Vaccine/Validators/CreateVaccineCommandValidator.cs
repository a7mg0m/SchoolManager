using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateVaccineCommandValidator : AbstractValidator<CreateVaccineCommand>
    {
        public CreateVaccineCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.TotalDoses).GreaterThan(0).WithMessage("TotalDosesInvalid");
        }
    }
}
