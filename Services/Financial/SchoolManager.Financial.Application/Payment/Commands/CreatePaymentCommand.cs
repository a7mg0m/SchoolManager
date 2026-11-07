using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CreatePaymentCommand(
        Guid StudentFeeId,
        Guid ParentId,
        Guid PaymentMethodId,
        decimal Amount,
        string Currency
    ) : ICommand<Guid>;
}
