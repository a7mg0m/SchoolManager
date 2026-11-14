using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Also activates/deactivates the employee's User, so a deactivated employee cannot log in.</summary>
    public record SetEmployeeActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
