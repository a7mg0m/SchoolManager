using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record UpdateRevenueCommand(
        Guid Id,
        string Title,
        string? Description,
        Guid CategoryId,
        RevenueType Type,
        decimal Amount,
        string Currency,
        DateTime RevenueDate,
        string? Reference
    ) : ICommand<bool>;
}
