namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>What the logged-in teacher teaches and the current academic year / semester (from the Management service).</summary>
    public sealed record TeacherClasses(Guid? AcademicYearId, Guid? SemesterId, IReadOnlyList<TeacherSubject> Subjects)
    {
        public TeacherSubject? Subject(Guid subjectId, Guid gradeId)
            => Subjects.FirstOrDefault(s => s.SubjectId == subjectId && s.GradeId == gradeId);
    }
}
