namespace SchoolManager.Management.Application
{
    /// <summary>A class (section) the teacher can give homework to.</summary>
    public class TeacherSectionDto
    {
        public Guid SectionId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
    }
}
