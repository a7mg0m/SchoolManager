namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A student in one of the logged-in teacher's classes (from the Management service).</summary>
    public sealed record SectionStudent(Guid Id, string NameAr, string NameEn);
}
