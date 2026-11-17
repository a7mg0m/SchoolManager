namespace SchoolManager.Management.Application
{
    /// <summary>
    /// A child's lessons for one day (parent app home): today while lessons remain, otherwise the next school day.
    /// </summary>
    public class ChildScheduleDto
    {
        /// <summary>Day shown (today, or the next day that has lessons).</summary>
        public DateOnly Date { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        /// <summary>False when today has no lessons left and <see cref="Date"/> is a later day.</summary>
        public bool IsToday { get; set; }
        /// <summary>School (server) time used for the statuses.</summary>
        public TimeOnly Now { get; set; }

        /// <summary>Lesson in progress now (only when <see cref="IsToday"/>).</summary>
        public Guid? CurrentSlotId { get; set; }
        /// <summary>Next lesson that has not started yet.</summary>
        public Guid? NextSlotId { get; set; }

        /// <summary>Lessons of the day, in time order. Empty when the section has no timetable.</summary>
        public List<ChildScheduleSlotDto> Slots { get; set; } = new();
    }
}
