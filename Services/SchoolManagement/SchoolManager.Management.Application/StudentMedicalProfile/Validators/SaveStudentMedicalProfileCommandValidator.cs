using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class SaveStudentMedicalProfileCommandValidator : AbstractValidator<SaveStudentMedicalProfileCommand>
    {
        public SaveStudentMedicalProfileCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentIdRequired");
            RuleFor(x => x.BloodType).MaximumLength(10).WithMessage("BloodTypeTooLong");
            RuleFor(x => x.EmergencyPhone).MaximumLength(50).WithMessage("EmergencyPhoneTooLong");
        }
    }
}
