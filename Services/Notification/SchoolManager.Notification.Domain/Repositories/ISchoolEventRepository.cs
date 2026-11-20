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
        Task<List<SchoolEvent>> GetForStudentAsync(Guid? academicYearId, Guid? semesterId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default);
    }

}
