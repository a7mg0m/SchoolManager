using FluentValidation;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
    {
        public UpdateExamCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectRequired");
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearRequired");
            RuleFor(x => x.SemesterId).NotEmpty().WithMessage("SemesterRequired");
        }
    }
}
