using FluentValidation;

namespace SchoolManager.Financial.Application
{
    public class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
    {
        public CreateExpenseCategoryCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }

    public class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
    {
        public UpdateExpenseCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }

    public class DeleteExpenseCategoryCommandValidator : AbstractValidator<DeleteExpenseCategoryCommand>
    {
        public DeleteExpenseCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
