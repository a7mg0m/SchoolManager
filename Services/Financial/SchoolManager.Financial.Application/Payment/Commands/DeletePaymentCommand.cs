using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeletePaymentCommand(Guid Id) : ICommand<bool>;
}
