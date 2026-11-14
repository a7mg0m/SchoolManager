using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetEmployeeGroupByIdQuery(Guid Id) : IQuery<EmployeeGroupDto?>;
}
