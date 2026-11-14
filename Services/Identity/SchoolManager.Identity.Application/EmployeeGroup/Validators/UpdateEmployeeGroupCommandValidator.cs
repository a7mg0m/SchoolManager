using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class UpdateEmployeeGroupCommandValidator : AbstractValidator<UpdateEmployeeGroupCommand>
    {
        public UpdateEmployeeGroupCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupRequired);
            RuleFor(x => x.NameAr).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupNameRequired).MaximumLength(200);
            RuleFor(x => x.NameEn).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupNameRequired).MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
