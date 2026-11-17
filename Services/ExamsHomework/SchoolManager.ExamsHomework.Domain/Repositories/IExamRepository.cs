using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public interface IExamRepository : IRepository<Exam>
    {
        /// <summary>
        /// Exams of a grade in an academic year that are for the whole grade or for the given section,
        /// with only the given student's results loaded (read-only).
        /// </summary>
        Task<List<Exam>> GetForStudentAsync(Guid academicYearId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default);
    }

}
