namespace SchoolManager.Management.Application
{
    /// <summary>A child's whole week (parent app timetable page): the lessons of every day that has any.</summary>
    public class ChildTimetableDto
    {
        public Guid StudentId { get; set; }
        /// <summary>School (server) day and time, so the app can show today and the lesson now.</summary>
        public DayOfWeek Today { get; set; }
        public TimeOnly Now { get; set; }
        /// <summary>Days with lessons, Saturday first; lessons in time order.</summary>
        public List<ChildTimetableDayDto> Days { get; set; } = new();
    }
}
