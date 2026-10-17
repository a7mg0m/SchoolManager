using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetAllRolesQueryHandler : ICommandHandler<GetAllRolesQuery, List<RoleDto>>
    {
        private readonly IRoleRepository _repository;

        public GetAllRolesQueryHandler(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _repository.GetAllAsync();

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                NameArabic = r.Name.Arabic,
                NameEnglish = r.Name.English,
                Description = r.Description
            }).ToList();
        }
    }
}
