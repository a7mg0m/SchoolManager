using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <summary>The marks of an event; null when the event does not exist</summary>
    public record GetEventAttendanceQuery(Guid EventId) : IQuery<List<EventAttendanceDto>?>;

    /// <summary>Saves marks of an event (adds, changes or clears each one); false when the event does not exist</summary>
    public record SaveEventAttendanceCommand(Guid EventId, IReadOnlyList<EventAttendanceInput> Records) : ICommand<bool>;
}
