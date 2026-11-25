namespace SchoolManager.Management.Application
{
    /// <summary>One day of the timetable's week (0 = Sunday … 6 = Saturday)</summary>
    public class TimetableWorkDayDto
    {
        public DayOfWeek Day { get; set; }
        public bool IsWorkDay { get; set; }
        public TimeOnly? BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; }

        /// <summary>Minutes of one period</summary>
        public int SlotDuration { get; set; }

        /// <summary>Number of periods in the day</summary>
        public int SlotCount { get; set; }
    }
}
