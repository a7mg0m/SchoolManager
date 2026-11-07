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

    public class DeleteStudentFeeCommandValidator : AbstractValidator<DeleteStudentFeeCommand>
    {
        public DeleteStudentFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
