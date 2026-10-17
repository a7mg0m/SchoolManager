using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class HomeworkPublishDto
    {
        public Guid Id { get; set; }
        public Guid HomeworkId { get; set; }
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
        public DateTime PublishedOn { get; set; }
        public HomeworkStatusEnum Status { get; set; }
    }
}
