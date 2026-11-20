namespace SchoolManager.Management.Application
{
    /// <summary>One lesson of the logged-in teacher: when, which subject and which class.</summary>
    public class TeacherTimetableSlotDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public Guid SectionId { get; set; }
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
    }
}
