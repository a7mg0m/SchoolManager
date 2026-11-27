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
            RuleFor(x => x.JobLevel).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.JobLevel)).WithMessage("JobLevelTooLong");
            RuleFor(x => x.JobTitle).MaximumLength(Domain.Teacher.MaxJobTitleLength).WithMessage("JobTitleTooLong");
            RuleFor(x => x.EmployeeNumber).MaximumLength(Domain.Teacher.MaxEmployeeNumberLength).WithMessage("EmployeeNumberTooLong");
            RuleFor(x => x.Qualification).MaximumLength(Domain.Teacher.MaxQualificationLength).WithMessage("QualificationTooLong");
            RuleFor(x => x.Specialization).MaximumLength(Domain.Teacher.MaxSpecializationLength).WithMessage("SpecializationTooLong");
            RuleFor(x => x.HireDate)
                .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("HireDateInFuture");
            RuleFor(x => x.YearsOfExperience)
                .InclusiveBetween(0, 60).When(x => x.YearsOfExperience.HasValue)
                .WithMessage("YearsOfExperienceInvalid");
            RuleFor(x => x.ServiceEndDate)
                .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.Today))
                .When(x => x.IsServiceEnded)
                .WithMessage("ServiceEndDateInFuture");
        }
    }
}
