using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkPublishDto
    {
        public Guid HomeworkId { get; set; }
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
        public HomeworkStatusEnum Status { get; set; }
    }
}
