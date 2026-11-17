namespace SchoolManager.Management.Application
{
    /// <summary>One day of a child's weekly timetable and its lessons in time order.</summary>
    public class ChildTimetableDayDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public List<ChildScheduleSlotDto> Slots { get; set; } = new();
    }
}
