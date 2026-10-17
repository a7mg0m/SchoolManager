namespace SchoolManager.Notification.Application
{
	public class EventTargetDto
	{
		public Guid Id { get; set; }
		public Guid EventId { get; set; }
		public Guid? GradeId { get; set; }
		public Guid? SectionId { get; set; }
		public Guid? TeacherId { get; set; }
		public Guid? StudentId { get; set; }
	}
}
