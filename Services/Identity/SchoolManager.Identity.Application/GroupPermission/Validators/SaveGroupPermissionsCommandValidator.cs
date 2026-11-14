using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class SaveGroupPermissionsCommandValidator : AbstractValidator<SaveGroupPermissionsCommand>
    {
        public SaveGroupPermissionsCommandValidator()
        {
            RuleFor(x => x.EmployeeGroupId).NotEmpty().WithMessage(ErrorKeys.EmployeeGroupRequired);
            RuleFor(x => x.Permissions).NotNull();
            RuleForEach(x => x.Permissions)
                .Must(p => p.MenuItemId != Guid.Empty).WithMessage(ErrorKeys.GroupPermissionMenuItemRequired);
            RuleFor(x => x.Permissions)
                .Must(list => list is null || list.Select(p => p.MenuItemId).Distinct().Count() == list.Count)
                .WithMessage(ErrorKeys.GroupPermissionDuplicateMenuItem);
        }
    }
}
