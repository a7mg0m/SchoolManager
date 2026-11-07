using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreateRevenueCommandValidator : AbstractValidator<CreateRevenueCommand>
    {
        public CreateRevenueCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("CategoryIdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class UpdateRevenueCommandValidator : AbstractValidator<UpdateRevenueCommand>
    {
        public UpdateRevenueCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("CategoryIdRequired");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class DeleteRevenueCommandValidator : AbstractValidator<DeleteRevenueCommand>
    {
        public DeleteRevenueCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
