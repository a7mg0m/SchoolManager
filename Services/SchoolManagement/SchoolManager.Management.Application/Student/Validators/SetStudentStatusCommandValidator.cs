using FluentValidation;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SetStudentStatusCommandValidator : AbstractValidator<SetStudentStatusCommand>
    {
        public SetStudentStatusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Status).IsInEnum().WithMessage("InvalidStatus");
            RuleFor(x => x.Note).MaximumLength(Domain.Student.MaxStatusNoteLength).WithMessage("StatusNoteTooLong");
            // Not in the future
            RuleFor(x => x.Date)
                .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("StatusDateInFuture");
        }
    }
}
