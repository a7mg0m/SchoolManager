using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Whole menu (including inactive items) with the group's flags, for a permission matrix screen.</summary>
    public class GetGroupPermissionsQueryHandler : IQueryHandler<GetGroupPermissionsQuery, GroupPermissionsDto?>
    {
        private readonly IGroupPermissionRepository _repository;
        private readonly IEmployeeGroupRepository _groupRepository;
        private readonly IMenuItemRepository _menuItemRepository;

        public GetGroupPermissionsQueryHandler(
            IGroupPermissionRepository repository,
            IEmployeeGroupRepository groupRepository,
            IMenuItemRepository menuItemRepository)
        {
            _repository = repository;
            _groupRepository = groupRepository;
            _menuItemRepository = menuItemRepository;
        }

        public async Task<GroupPermissionsDto?> Handle(GetGroupPermissionsQuery query, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.GetByIdAsync(query.EmployeeGroupId);
            if (group is null)
                return null;

            var menu = await _menuItemRepository.GetAllOrderedAsync();
            var rows = (await _repository.GetByGroupAsync(group.Id)).ToDictionary(r => r.MenuItemId);

            return new GroupPermissionsDto
            {
                EmployeeGroupId = group.Id,
                EmployeeGroupNameAr = group.Name.Arabic,
                EmployeeGroupNameEn = group.Name.English,
                MenuItems = PermissionTreeBuilder.Build(
                    menu,
                    id => rows.TryGetValue(id, out var r)
                        ? new PermissionTreeBuilder.Flags(r.CanView, r.CanAdd, r.CanEdit, r.CanDelete)
                        : PermissionTreeBuilder.Flags.None,
                    activeOnly: false)
            };
        }
    }
}
