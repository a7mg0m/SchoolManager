using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>A notification was already created from this integration event</summary>
        Task<bool> ExistsForSourceEventAsync(Guid eventId, CancellationToken cancellationToken = default);

        /// <summary>Admin notifications (not the system ones) with their targets, of one academic year (null: all)</summary>
        Task<List<Notification>> GetForYearAsync(Guid? academicYearId, CancellationToken cancellationToken = default);

        /// <summary>The latest school-office notifications (home dashboard)</summary>
        Task<List<Notification>> GetLatestAdminAsync(int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifications already sent (on or before <paramref name="today"/>) that concern a student: for the whole school
        /// (no targets), its grade, its class or the student itself; of one academic year when given. Read-only, with targets.
        /// </summary>
        Task<List<Notification>> GetForStudentAsync(Guid? academicYearId, Guid gradeId, Guid sectionId, Guid studentId, DateOnly today, CancellationToken cancellationToken = default);

        /// <summary>Notifications already sent for the whole school or for this teacher; of one academic year when given. Read-only, with targets.</summary>
        Task<List<Notification>> GetForTeacherAsync(Guid? academicYearId, Guid teacherId, DateOnly today, CancellationToken cancellationToken = default);

        /// <summary>Notifications already sent for the whole school or for this bus's driver; of one academic year when given. Read-only, with targets.</summary>
        Task<List<Notification>> GetForDriverAsync(Guid? academicYearId, Guid busId, DateOnly today, CancellationToken cancellationToken = default);

        /// <summary>Notifications due (SentOn on or before <paramref name="today"/>) and not pushed yet, with targets (tracked)</summary>
        Task<List<Notification>> GetDueForPushAsync(DateOnly today, CancellationToken cancellationToken = default);
    }
   
}
