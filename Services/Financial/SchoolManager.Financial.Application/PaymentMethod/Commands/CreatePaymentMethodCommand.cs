using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record CreatePaymentMethodCommand(
        string Name,
        PaymentType Type,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
