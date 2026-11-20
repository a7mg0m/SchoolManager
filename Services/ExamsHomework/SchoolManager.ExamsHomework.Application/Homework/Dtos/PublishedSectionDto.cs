namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A class a homework was published to</summary>
    public class PublishedSectionDto
    {
        public Guid SectionId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public DateTime PublishedOn { get; set; }
    }
}
