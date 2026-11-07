using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentCommandValidator()
        {
            RuleFor(x => x.StudentFeeId).NotEmpty().WithMessage("StudentFeeIdRequired");
            RuleFor(x => x.PaymentMethodId).NotEmpty().WithMessage("PaymentMethodIdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
        }
    }

    public class CompletePaymentCommandValidator : AbstractValidator<CompletePaymentCommand>
    {
        public CompletePaymentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.TransactionReference).NotEmpty().WithMessage("TransactionReferenceRequired");
        }
    }

    public class FailPaymentCommandValidator : AbstractValidator<FailPaymentCommand>
    {
        public FailPaymentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("ReasonRequired");
        }
    }

    public class DeletePaymentCommandValidator : AbstractValidator<DeletePaymentCommand>
    {
        public DeletePaymentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
