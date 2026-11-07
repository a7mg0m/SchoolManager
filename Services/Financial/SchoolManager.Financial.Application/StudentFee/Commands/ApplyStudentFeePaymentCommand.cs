using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record ApplyStudentFeePaymentCommand(
        Guid Id,
        decimal Amount,
        string Currency
    ) : ICommand<bool>;
}
