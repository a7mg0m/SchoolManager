using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class CreateEmployeeGroupCommandValidator : AbstractValidator<CreateEmployeeGroupCommand>
    {
        public CreateEmployeeGroupCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupNameRequired).MaximumLength(200);
            RuleFor(x => x.NameEn).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupNameRequired).MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
