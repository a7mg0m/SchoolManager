using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateAcademicYearCommandValidator : AbstractValidator<UpdateAcademicYearCommand>
    {
        public UpdateAcademicYearCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }
}
