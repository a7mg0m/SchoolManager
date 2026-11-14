using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetActiveEmployeeGroupsQuery() : IQuery<List<EmployeeGroupDto>>;
}
