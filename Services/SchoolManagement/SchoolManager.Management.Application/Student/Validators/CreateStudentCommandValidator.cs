using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentCommandValidator()
        {
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("FullNameArRequired");
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage("FullNameEnRequired");
            RuleFor(x => x.NationalId).NotEmpty().WithMessage("NationalIdRequired");
            RuleFor(x => x.Gender).NotEmpty().WithMessage("GenderRequired");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumberRequired");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("EmailInvalid");
            RuleFor(x => x.Address).NotEmpty().WithMessage("AddressRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.ParentId).NotEmpty().WithMessage("ParentIdRequired");
        }
    }
}
