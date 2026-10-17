namespace SchoolManager.Notification.Application
{
    public class TargetInputDto
    {
        public Guid? GradeId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid? StudentId { get; set; }
    }
}
