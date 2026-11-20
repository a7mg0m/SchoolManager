using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public interface ISchoolEventRepository : IRepository<SchoolEvent>
    {
        /// <summary>
        /// Events of an academic year that concern a student: for everyone, for its stage or grade
        /// (stages are saved as their grades), for its section, or for the student itself. Read-only, without targets.
        /// </summary>
        /// <summary>
        /// Events that concern a student; only those of <paramref name="academicYearId"/> / <paramref name="semesterId"/>
        /// when given (chosen in the parent app), else of every year / semester.
        /// </summary>
        /// <summary>
        /// Events for a teacher: for everyone, or for teachers when the teacher is one of the targets (no targets = all
        /// teachers); only those of <paramref name="academicYearId"/> / <paramref name="semesterId"/> when given. Read-only.
        /// </summary>
        Task<List<SchoolEvent>> GetForTeacherAsync(Guid? academicYearId, Guid? semesterId, Guid teacherId, CancellationToken cancellationToken = default);

        Task<List<SchoolEvent>> GetForStudentAsync(Guid? academicYearId, Guid? semesterId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default);
    }

}
