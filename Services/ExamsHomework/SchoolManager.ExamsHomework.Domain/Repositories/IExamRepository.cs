using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    /// <summary>The results of one exam in numbers (home dashboard)</summary>
    public sealed record ExamResultSummary(Guid ExamId, int Count, int Passed, double AverageScore);

    public interface IExamRepository : IRepository<Exam>
    {
        /// <summary>
        /// Exams of a grade in an academic year that are for the whole grade or for the given section,
        /// with only the given student's results loaded (read-only).
        /// </summary>
        /// <summary>Exams a teacher added, with their results (read-only); optionally of one academic year / semester</summary>
        Task<List<Exam>> GetForTeacherAsync(Guid teacherId, Guid? academicYearId, Guid? semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The teacher's exams and every exam of the given subjects in the given grades (by the school office or
        /// another teacher), with their results (read-only); optionally of one academic year / semester. The caller
        /// keeps the subject / grade / class combinations the teacher really teaches.
        /// </summary>
        Task<List<Exam>> GetForTeacherClassesAsync(Guid teacherId, IReadOnlyCollection<Guid> subjectIds, IReadOnlyCollection<Guid> gradeIds,
            Guid? academicYearId, Guid? semesterId, CancellationToken cancellationToken = default);

        /// <summary>The Final and Second final exams of a grade in an academic year, with their results (read-only)</summary>
        Task<List<Exam>> GetFinalsWithResultsAsync(Guid academicYearId, Guid gradeId, CancellationToken cancellationToken = default);

        /// <summary>Per exam with results: how many, how many passed, the average score (computed by SQL)</summary>
        Task<List<ExamResultSummary>> GetResultSummariesAsync(CancellationToken cancellationToken = default);

        /// <summary>One exam with all its results (read-only)</summary>
        Task<Exam?> GetWithResultsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<Exam>> GetForStudentAsync(Guid academicYearId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default);
    }

}
