using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>One person's mark at an event</summary>
    public record EventAttendanceDto(EventAttendeeType AttendeeType, Guid AttendeeId, bool IsPresent, DateTime MarkedOn);

    /// <summary>A mark to save; IsPresent null clears it (back to "not marked")</summary>
    public record EventAttendanceInput(EventAttendeeType AttendeeType, Guid AttendeeId, bool? IsPresent);

    /// <summary>Body of PUT /api/school-event/{id}/attendance</summary>
    public class SaveEventAttendanceDto
    {
        public List<EventAttendanceInput> Records { get; set; } = new();
    }
}
