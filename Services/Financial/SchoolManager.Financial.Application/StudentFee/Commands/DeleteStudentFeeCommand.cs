using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteStudentFeeCommand(Guid Id) : ICommand<bool>;
}
