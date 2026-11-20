using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class AssignStudentsToBusCommandValidator : AbstractValidator<AssignStudentsToBusCommand>
    {
        public AssignStudentsToBusCommandValidator()
        {
            RuleFor(x => x.BusId).NotEmpty().WithMessage("BusIdRequired");
            RuleFor(x => x.AcademicYearId).NotEmpty().WithMessage("AcademicYearIdRequired");
            RuleFor(x => x.StudentIds).NotEmpty().WithMessage("StudentIdsRequired");
            RuleFor(x => x.FeeAmount).GreaterThan(0).WithMessage("FeeAmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
            RuleFor(x => x.StartDate).NotEmpty().WithMessage("StartDateRequired");
            RuleFor(x => x.DueDate).NotEmpty().WithMessage("DueDateRequired");
        }
    }

    public class ChangeBusFeeCommandValidator : AbstractValidator<ChangeBusFeeCommand>
    {
        public ChangeBusFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.FeeAmount).GreaterThan(0).WithMessage("FeeAmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
        }
    }

    public class MoveStudentToBusCommandValidator : AbstractValidator<MoveStudentToBusCommand>
    {
        public MoveStudentToBusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.BusId).NotEmpty().WithMessage("BusIdRequired");
        }
    }

    public class EndBusAssignmentCommandValidator : AbstractValidator<EndBusAssignmentCommand>
    {
        public EndBusAssignmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.EndDate).NotEmpty().WithMessage("EndDateRequired");
        }
    }
}
