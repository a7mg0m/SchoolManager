using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CompletePaymentCommand(Guid Id, string TransactionReference) : ICommand<bool>;

    public record FailPaymentCommand(Guid Id, string Reason) : ICommand<bool>;
}
