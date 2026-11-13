using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record CreateFeeCommand(
        Guid GradeId,
        string Name,
        FeeType Type,
        decimal Amount,
        string Currency,
        bool IsMandatory
    ) : ICommand<Guid>;
}
