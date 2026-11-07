using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
    {
        public CreatePaymentMethodCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
    {
        public UpdatePaymentMethodCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TypeInvalid");
        }
    }

    public class DeletePaymentMethodCommandValidator : AbstractValidator<DeletePaymentMethodCommand>
    {
        public DeletePaymentMethodCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
