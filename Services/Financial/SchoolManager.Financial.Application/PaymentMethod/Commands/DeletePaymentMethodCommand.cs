using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeletePaymentMethodCommand(Guid Id) : ICommand<bool>;
}
