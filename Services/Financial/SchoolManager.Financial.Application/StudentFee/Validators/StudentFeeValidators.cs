using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class ApplyStudentFeePaymentCommandValidator : AbstractValidator<ApplyStudentFeePaymentCommand>
    {
        public ApplyStudentFeePaymentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
        }
    }

    public class WaiveStudentFeeCommandValidator : AbstractValidator<WaiveStudentFeeCommand>
    {
        public WaiveStudentFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("ReasonRequired")
                .MaximumLength(Domain.StudentFee.MaxWaiveReasonLength).WithMessage("ReasonTooLong");
        }
    }

    public class DeleteStudentFeeCommandValidator : AbstractValidator<DeleteStudentFeeCommand>
    {
        public DeleteStudentFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
