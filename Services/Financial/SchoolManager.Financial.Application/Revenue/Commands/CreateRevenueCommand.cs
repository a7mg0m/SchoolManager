using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record CreateRevenueCommand(
        string Title,
        string? Description,
        Guid CategoryId,
        RevenueType Type,
        decimal Amount,
        string Currency,
        DateTime RevenueDate,
        string? Reference,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
