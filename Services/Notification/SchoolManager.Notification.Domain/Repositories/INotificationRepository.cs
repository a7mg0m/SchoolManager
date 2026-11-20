using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>Notifications with their targets, of one academic year (null: all)</summary>
        Task<List<Notification>> GetForYearAsync(Guid? academicYearId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifications already sent (on or before <paramref name="today"/>) that concern a student: for the whole school
        /// (no targets), its grade, its class or the student itself; of one academic year when given. Read-only, with targets.
        /// </summary>
        Task<List<Notification>> GetForStudentAsync(Guid? academicYearId, Guid gradeId, Guid sectionId, Guid studentId, DateOnly today, CancellationToken cancellationToken = default);

        /// <summary>Notifications already sent for the whole school or for this teacher; of one academic year when given. Read-only, with targets.</summary>
        Task<List<Notification>> GetForTeacherAsync(Guid? academicYearId, Guid teacherId, DateOnly today, CancellationToken cancellationToken = default);
    }
   
}
