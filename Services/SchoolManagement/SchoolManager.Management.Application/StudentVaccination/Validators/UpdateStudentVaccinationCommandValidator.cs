using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateStudentVaccinationCommandValidator : AbstractValidator<UpdateStudentVaccinationCommand>
    {
        public UpdateStudentVaccinationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.VaccineId).NotEmpty().WithMessage("VaccineIdRequired");
            RuleFor(x => x.DoseNumber).GreaterThan(0).WithMessage("DoseNumberInvalid");
            RuleFor(x => x.Status).IsInEnum().WithMessage("StatusInvalid");
            RuleFor(x => x.Place).IsInEnum().WithMessage("PlaceInvalid");
            RuleFor(x => x.VaccinationDate).NotNull().When(x => x.Status == Domain.VaccinationStatus.Given).WithMessage("VaccinationDateRequired");
        }
    }
}
