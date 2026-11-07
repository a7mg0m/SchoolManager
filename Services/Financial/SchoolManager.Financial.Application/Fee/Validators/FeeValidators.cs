using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreateFeeCommandValidator : AbstractValidator<CreateFeeCommand>
    {
        public CreateFeeCommandValidator()
        {
            RuleFor(x => x.GradeId).NotEmpty().WithMessage("GradeIdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
        }
    }

    public class UpdateFeeCommandValidator : AbstractValidator<UpdateFeeCommand>
    {
        public UpdateFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AmountMustBeGreaterThanZero");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("CurrencyRequired");
        }
    }

    public class DeleteFeeCommandValidator : AbstractValidator<DeleteFeeCommand>
    {
        public DeleteFeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
