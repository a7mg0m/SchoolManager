using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetEmployeeByIdQuery(Guid Id) : IQuery<EmployeeDto?>;
}
