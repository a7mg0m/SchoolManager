namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>Classes to publish a homework to</summary>
    public class PublishHomeworkDto
    {
        public List<Guid> SectionIds { get; set; } = new();
    }
}
