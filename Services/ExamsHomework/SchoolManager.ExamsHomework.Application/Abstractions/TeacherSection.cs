namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A class (section) the logged-in teacher can give homework to (from the Management service).</summary>
    public sealed record TeacherSection(Guid Id, string NameAr, string NameEn);
}
