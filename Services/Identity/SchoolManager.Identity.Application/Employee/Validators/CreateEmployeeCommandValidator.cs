using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.FullNameAr).NotEmpty().WithMessage(ErrorKeys.FullNameRequired).MaximumLength(200);
            RuleFor(x => x.FullNameEn).NotEmpty().WithMessage(ErrorKeys.FullNameRequired).MaximumLength(200);
            RuleFor(x => x.NationalId).NotEmpty().WithMessage(ErrorKeys.NationalIdRequired).MaximumLength(50);
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(ErrorKeys.PhoneNumberRequired).MaximumLength(50);
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorKeys.EmailRequired)
                .EmailAddress().WithMessage(ErrorKeys.EmailInvalid)
                .MaximumLength(256);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.JobTitle).NotEmpty().WithMessage(ErrorKeys.JobTitleRequired).MaximumLength(150);
            RuleFor(x => x.HireDate).NotEqual(default(DateOnly)).WithMessage(ErrorKeys.HireDateRequired);
            RuleFor(x => x.EmployeeGroupId).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupRequired);
            RuleFor(x => x.LoginName)
                .NotEmpty().WithMessage(ErrorKeys.LoginNameRequired)
                .MinimumLength(3).WithMessage(ErrorKeys.LoginNameTooShort)
                .MaximumLength(100);
            RuleFor(x => x.Password).NotEmpty().WithMessage(ErrorKeys.PasswordRequired);
        }
    }
}
