using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    /// <summary>
    /// Assigns the selected fees to one or more students.
    /// One StudentFee is created per student, totalling the selected fees.
    /// </summary>
    public record AssignStudentFeesCommand(
        List<Guid> StudentIds,
        List<Guid> FeeIds,
        decimal DiscountAmount,
        DateTime DueDate
    ) : ICommand<List<Guid>>;
}
