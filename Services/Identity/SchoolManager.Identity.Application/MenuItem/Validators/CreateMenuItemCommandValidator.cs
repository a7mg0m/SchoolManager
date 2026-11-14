using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage(ErrorKeys.MenuItemNameRequired).MaximumLength(200);
            RuleFor(x => x.NameEn).NotEmpty().WithMessage(ErrorKeys.MenuItemNameRequired).MaximumLength(200);
            RuleFor(x => x.Icon).MaximumLength(100);
            RuleFor(x => x.Route)
                .MaximumLength(300)
                .Must(MenuItemRules.IsValidRoute).WithMessage(ErrorKeys.MenuItemRouteInvalid);
            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage(ErrorKeys.MenuItemOrderInvalid)
                .When(x => x.Order.HasValue);
            RuleFor(x => x.ParentId).NotEqual(Guid.Empty).WithMessage(ErrorKeys.MenuItemParentNotFound)
                .When(x => x.ParentId.HasValue);
        }
    }
}
