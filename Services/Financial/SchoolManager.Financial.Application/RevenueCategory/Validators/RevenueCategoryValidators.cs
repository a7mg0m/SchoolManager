using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreateRevenueCategoryCommandValidator : AbstractValidator<CreateRevenueCategoryCommand>
    {
        public CreateRevenueCategoryCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }

    public class UpdateRevenueCategoryCommandValidator : AbstractValidator<UpdateRevenueCategoryCommand>
    {
        public UpdateRevenueCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }

    public class DeleteRevenueCategoryCommandValidator : AbstractValidator<DeleteRevenueCategoryCommand>
    {
        public DeleteRevenueCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
