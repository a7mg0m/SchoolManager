using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Deletes the employee, its User and its UserProfile.</summary>
    public record DeleteEmployeeCommand(Guid Id) : ICommand<bool>;
}
