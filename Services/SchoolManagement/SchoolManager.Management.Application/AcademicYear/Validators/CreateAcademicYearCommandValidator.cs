using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateAcademicYearCommandValidator : AbstractValidator<CreateAcademicYearCommand>
    {
        public CreateAcademicYearCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }
}
