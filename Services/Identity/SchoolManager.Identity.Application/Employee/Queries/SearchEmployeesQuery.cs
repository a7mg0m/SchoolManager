using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record SearchEmployeesQuery(string SearchTerm) : IQuery<List<EmployeeDto>>;
}
