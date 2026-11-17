using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// True when no user other than <paramref name="UserId"/> has the login name
    /// (e.g. the admin parent page checks a parent's login name; a parent's user id equals the parent id).
    /// </summary>
    public record IsLoginNameAvailableQuery(string LoginName, Guid? UserId) : ICommand<bool>;

    public class IsLoginNameAvailableQueryHandler : ICommandHandler<IsLoginNameAvailableQuery, bool>
    {
        private readonly IUserRepository _repository;

        public IsLoginNameAvailableQueryHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(IsLoginNameAvailableQuery request, CancellationToken cancellationToken)
        {
            var loginName = request.LoginName?.Trim() ?? string.Empty;
            if (loginName.Length == 0)
                return false;

            var users = await _repository.SearchAsync(u => u.LoginName == loginName);
            return users.All(u => u.Id == request.UserId);
        }
    }
}
