using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteFeeCommand(Guid Id) : ICommand<bool>;
}
