using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("CategoryIdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
    {
        public UpdateExpenseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("CategoryIdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
    {
        public DeleteExpenseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
