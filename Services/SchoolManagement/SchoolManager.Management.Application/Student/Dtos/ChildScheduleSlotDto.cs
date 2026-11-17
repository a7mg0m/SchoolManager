namespace SchoolManager.Management.Application
{
    /// <summary>One lesson of a child's timetable (parent app).</summary>
    public class ChildScheduleSlotDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public string TeacherNameAr { get; set; } = string.Empty;
        public string TeacherNameEn { get; set; } = string.Empty;
        /// <summary>Finished, Current or Upcoming (see <see cref="SlotStatus"/>).</summary>
        public string Status { get; set; } = SlotStatus.Upcoming;
    }
}
