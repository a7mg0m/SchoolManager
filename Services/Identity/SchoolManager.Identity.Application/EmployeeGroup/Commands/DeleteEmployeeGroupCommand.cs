using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Fails with <see cref="ErrorKeys.EmployeeGroupHasEmployees"/> while the group still has employees.</summary>
    public record DeleteEmployeeGroupCommand(Guid Id) : ICommand<bool>;
}
