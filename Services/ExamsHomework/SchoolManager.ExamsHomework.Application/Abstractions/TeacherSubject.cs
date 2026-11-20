namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A subject the logged-in teacher teaches, its grade and the grade's classes (from the Management service).</summary>
    public sealed record TeacherSubject(
        Guid SubjectId,
        string SubjectNameAr,
        string SubjectNameEn,
        Guid GradeId,
        string GradeNameAr,
        string GradeNameEn,
        IReadOnlyList<TeacherSection> Sections);
}
