using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Menu of the logged-in user:
    /// - employee: active items their group may view (plus the parent groups of those items), with the group's flags;
    ///   nothing when the employee or the group is inactive.
    /// - not an employee with role SystemAdmin or Admin: the whole active menu with every permission.
    /// - anyone else (teacher, student, parent, ...): no menu items.
    /// </summary>
    public class GetMyMenuQueryHandler : IQueryHandler<GetMyMenuQuery, MyMenuDto>
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeGroupRepository _groupRepository;
        private readonly IGroupPermissionRepository _permissionRepository;
        private readonly IMenuItemRepository _menuItemRepository;

        public GetMyMenuQueryHandler(
            ICurrentUserService currentUser,
            IUserRepository userRepository,
            IEmployeeRepository employeeRepository,
            IEmployeeGroupRepository groupRepository,
            IGroupPermissionRepository permissionRepository,
            IMenuItemRepository menuItemRepository)
        {
            _currentUser = currentUser;
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
            _groupRepository = groupRepository;
            _permissionRepository = permissionRepository;
            _menuItemRepository = menuItemRepository;
        }

        public async Task<MyMenuDto> Handle(GetMyMenuQuery query, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated)
                throw new DomainException(ErrorKeys.UserNotAuthenticated);

            var menu = await _menuItemRepository.GetAllOrderedAsync();
            var employee = (await _employeeRepository.SearchAsync(e => e.UserId == _currentUser.UserId)).FirstOrDefault();

            if (employee is null)
            {
                var user = await _userRepository.GetByIdAsync(_currentUser.UserId);
                var isAdmin = user is not null && user.IsActive &&
                              (user.RoleId == RoleIds.SystemAdmin || user.RoleId == RoleIds.Admin);

                return isAdmin
                    ? new MyMenuDto
                    {
                        IsFullAccess = true,
                        MenuItems = PermissionTreeBuilder.Build(menu, _ => PermissionTreeBuilder.Flags.All, activeOnly: true)
                    }
                    : new MyMenuDto { IsFullAccess = false }; // teachers, students, parents: no admin menu
            }

            var result = new MyMenuDto { IsFullAccess = false, EmployeeGroupId = employee.EmployeeGroupId };

            var group = await _groupRepository.GetByIdAsync(employee.EmployeeGroupId);
            if (!employee.IsActive || group is null || !group.IsActive)
                return result; // no menu

            var rows = (await _permissionRepository.GetByGroupAsync(group.Id))
                .Where(r => r.CanView)
                .ToDictionary(r => r.MenuItemId);

            var visible = PermissionTreeBuilder.WithAncestors(menu, rows.Keys);

            result.MenuItems = PermissionTreeBuilder.Build(
                menu,
                id => rows.TryGetValue(id, out var r)
                    ? new PermissionTreeBuilder.Flags(r.CanView, r.CanAdd, r.CanEdit, r.CanDelete)
                    // A parent group shown only because of its children: visible, no actions
                    : new PermissionTreeBuilder.Flags(true, false, false, false),
                activeOnly: true,
                visible: visible);

            return result;
        }
    }
}
