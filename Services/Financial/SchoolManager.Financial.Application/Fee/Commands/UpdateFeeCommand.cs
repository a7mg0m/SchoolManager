using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record UpdateFeeCommand(
        Guid Id,
        string Name,
        FeeType Type,
        decimal Amount,
        string Currency,
        bool IsMandatory
    ) : ICommand<bool>;
}
