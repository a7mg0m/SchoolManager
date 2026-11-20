namespace SchoolManager.Management.Application
{
    /// <summary>The logged-in teacher's week (teacher app timetable) for an academic year and semester.</summary>
    public class TeacherTimetableDto
    {
        /// <summary>The academic year / semester the lessons are for (null when none exists)</summary>
        public Guid? AcademicYearId { get; set; }
        public Guid? SemesterId { get; set; }

        /// <summary>School (server) day and time, so the app can show today and the lesson now.</summary>
        public DayOfWeek Today { get; set; }
        public TimeOnly Now { get; set; }

        /// <summary>Days with lessons, Saturday first; lessons in time order.</summary>
        public List<TeacherTimetableDayDto> Days { get; set; } = new();
    }
}
