using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetPaymentByIdQuery(Guid Id) : IQuery<PaymentDto?>;

    public record GetPaymentsByStudentFeeQuery(Guid StudentFeeId) : IQuery<List<PaymentDto>>;

    public record GetAllPaymentsQuery() : IQuery<List<PaymentDto>>;
}
