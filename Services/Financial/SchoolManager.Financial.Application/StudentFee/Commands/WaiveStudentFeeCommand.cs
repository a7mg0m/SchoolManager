using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    /// <summary>Forgives the remaining balance of a student fee.</summary>
    public record WaiveStudentFeeCommand(Guid Id, string Reason) : ICommand<bool>;

    /// <summary>Undoes a waiver so the balance becomes payable again.</summary>
    public record RestoreStudentFeeWaiverCommand(Guid Id) : ICommand<bool>;
}
