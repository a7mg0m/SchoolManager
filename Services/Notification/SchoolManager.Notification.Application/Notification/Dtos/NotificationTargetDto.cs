namespace SchoolManager.Notification.Application
{
	public class NotificationTargetDto
	{
		public Guid Id { get; set; }
		public Guid NotificationId { get; set; }
		public Guid? GradeId { get; set; }
		public Guid? SectionId { get; set; }
		public Guid? TeacherId { get; set; }
		public Guid? StudentId { get; set; }
	}
}
