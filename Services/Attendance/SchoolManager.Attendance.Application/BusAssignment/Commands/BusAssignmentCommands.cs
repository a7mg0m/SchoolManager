using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Assigns students to a bus for an academic year, each with the bus fee entered here.
    /// <para>
    /// The fee is NOT read from the Financial grade fees of the academic year: one bus carries students of
    /// different grades. Each assignment is sent to Financial (BusFeeIntegrationEvent, Assigned) to become a
    /// Transportation StudentFee due on <paramref name="DueDate"/>.
    /// </para>
    /// </summary>
    public record AssignStudentsToBusCommand(
        Guid BusId,
        Guid AcademicYearId,
        Guid? BusRouteStopId,
        List<Guid> StudentIds,
        decimal FeeAmount,
        string Currency,
        DateOnly StartDate,
        DateTime DueDate
    ) : ICommand<List<Guid>>;

    /// <summary>New bus fee of the student (sent to Financial: FeeChanged)</summary>
    public record ChangeBusFeeCommand(Guid Id, decimal FeeAmount, string Currency) : ICommand<bool>;

    /// <summary>Moves the student to another bus / stop (the new bus must have a free seat)</summary>
    public record MoveStudentToBusCommand(Guid Id, Guid BusId, Guid? BusRouteStopId) : ICommand<bool>;

    /// <summary>The student stops riding the bus after <paramref name="EndDate"/> (sent to Financial: Ended)</summary>
    public record EndBusAssignmentCommand(Guid Id, DateOnly EndDate) : ICommand<bool>;
}
