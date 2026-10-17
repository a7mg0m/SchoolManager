using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateTimetableCommandValidator : AbstractValidator<UpdateTimetableCommand>
    {
        public UpdateTimetableCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
        }
    }
}
