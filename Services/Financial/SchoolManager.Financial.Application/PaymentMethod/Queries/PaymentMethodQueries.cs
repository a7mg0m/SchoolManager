using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetPaymentMethodByIdQuery(Guid Id) : IQuery<PaymentMethodDto?>;

    public record GetAllPaymentMethodsQuery() : IQuery<List<PaymentMethodDto>>;
}
