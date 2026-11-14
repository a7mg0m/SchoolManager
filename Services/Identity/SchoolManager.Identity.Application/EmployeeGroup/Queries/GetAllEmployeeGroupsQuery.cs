using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetAllEmployeeGroupsQuery() : IQuery<List<EmployeeGroupDto>>;
}
