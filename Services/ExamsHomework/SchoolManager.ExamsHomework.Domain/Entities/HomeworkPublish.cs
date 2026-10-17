using BuildingBlocks;


namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class HomeworkPublish : BaseEntity
    {
        public Guid HomeworkId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid SectionId { get; private set; }
        public DateTime PublishedOn { get; private set; }
        public HomeworkStatusEnum Status { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private HomeworkPublish() { }

        public HomeworkPublish(Guid homeworkId, Guid gradeId, Guid sectionId, HomeworkStatusEnum status, Guid createdBy)
        {
            HomeworkId = homeworkId;
            GradeId = gradeId;
            SectionId = sectionId;
            PublishedOn = DateTime.UtcNow;
            Status = status;

            AuditInfo = new AuditInfo(DateTime.UtcNow, createdBy);
        }
    }

}
