using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetEmployeesByGroupIdQuery(Guid EmployeeGroupId) : IQuery<List<EmployeeDto>>;
}
