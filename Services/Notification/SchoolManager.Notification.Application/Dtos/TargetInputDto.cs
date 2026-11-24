namespace SchoolManager.Notification.Application
{
    public class TargetInputDto
    {
        public Guid? GradeId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid? StudentId { get; set; }

        /// <summary>Notifications only: a bus, for its driver (school events ignore it)</summary>
        public Guid? BusId { get; set; }
    }
}
