using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Makes the group's permission rows match the given list in one SaveChanges:
    /// new items are added, changed ones updated, and items that are missing from the list
    /// or have all flags false are removed. Unknown menu item ids are ignored.
    /// </summary>
    public class SaveGroupPermissionsCommandHandler : ICommandHandler<SaveGroupPermissionsCommand, SaveGroupPermissionsSummary?>
    {
        private readonly IGroupPermissionRepository _repository;
        private readonly IEmployeeGroupRepository _groupRepository;
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SaveGroupPermissionsCommandHandler(
            IGroupPermissionRepository repository,
            IEmployeeGroupRepository groupRepository,
            IMenuItemRepository menuItemRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _groupRepository = groupRepository;
            _menuItemRepository = menuItemRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<SaveGroupPermissionsSummary?> Handle(SaveGroupPermissionsCommand command, CancellationToken cancellationToken)
        {
            if (await _groupRepository.GetByIdAsync(command.EmployeeGroupId) is null)
                return null;

            var menuItemIds = (await _menuItemRepository.GetAllAsync()).Select(m => m.Id).ToHashSet();

            // Wanted permissions: known menu items with at least one flag
            var wanted = command.Permissions
                .Where(p => menuItemIds.Contains(p.MenuItemId))
                .Where(p => p.CanView || p.CanAdd || p.CanEdit || p.CanDelete)
                .GroupBy(p => p.MenuItemId)
                .ToDictionary(g => g.Key, g => g.First());

            var existing = await _repository.GetByGroupAsync(command.EmployeeGroupId);
            var now = DateTime.Now;
            int created = 0, updated = 0, removed = 0;

            foreach (var row in existing)
            {
                if (!wanted.TryGetValue(row.MenuItemId, out var p))
                {
                    _repository.Remove(row);
                    removed++;
                }
                else if (row.Differs(p.CanView, p.CanAdd, p.CanEdit, p.CanDelete))
                {
                    row.Update(p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, row.AuditInfo.Updated(now, _currentUser.UserId));
                    updated++;
                }
            }

            var existingIds = existing.Select(r => r.MenuItemId).ToHashSet();
            foreach (var p in wanted.Values.Where(p => !existingIds.Contains(p.MenuItemId)))
            {
                // A new AuditInfo per row (owned type, must not be shared)
                await _repository.AddAsync(new GroupPermission(
                    command.EmployeeGroupId, p.MenuItemId,
                    p.CanView, p.CanAdd, p.CanEdit, p.CanDelete,
                    new AuditInfo(now, _currentUser.UserId)));
                created++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new SaveGroupPermissionsSummary(created, updated, removed);
        }
    }
}
