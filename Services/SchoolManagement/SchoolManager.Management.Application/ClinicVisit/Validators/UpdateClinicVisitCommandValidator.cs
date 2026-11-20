using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateClinicVisitCommandValidator : AbstractValidator<UpdateClinicVisitCommand>
    {
        public UpdateClinicVisitCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.DoctorName).NotEmpty().WithMessage("DoctorNameRequired");
            RuleFor(x => x.DoctorName).MaximumLength(200).WithMessage("DoctorNameTooLong");
            RuleFor(x => x.VisitDate).NotEmpty().WithMessage("VisitDateRequired");
            RuleFor(x => x.Complaint).NotEmpty().WithMessage("ComplaintRequired");
            RuleFor(x => x.Outcome).IsInEnum().WithMessage("OutcomeInvalid");
            RuleForEach(x => x.Medications).Must(m => !string.IsNullOrWhiteSpace(m.MedicineName)).WithMessage("MedicineNameRequired");
            RuleForEach(x => x.Medications).Must(m => !string.IsNullOrWhiteSpace(m.Dose)).WithMessage("DoseRequired");
        }
    }
}
