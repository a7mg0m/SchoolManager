namespace SchoolManager.Management.Application
{
    /// <summary>One day of the teacher's week and its lessons in time order.</summary>
    public class TeacherTimetableDayDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public List<TeacherTimetableSlotDto> Slots { get; set; } = new();
    }
}
