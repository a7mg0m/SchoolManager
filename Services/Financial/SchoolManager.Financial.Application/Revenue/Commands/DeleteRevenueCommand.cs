using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteRevenueCommand(Guid Id) : ICommand<bool>;
}
