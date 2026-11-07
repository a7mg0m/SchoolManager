using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record UpdatePaymentMethodCommand(
        Guid Id,
        string Name,
        PaymentType Type
    ) : ICommand<bool>;
}
