using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetAllEmployeesQuery() : IQuery<List<EmployeeDto>>;
}
