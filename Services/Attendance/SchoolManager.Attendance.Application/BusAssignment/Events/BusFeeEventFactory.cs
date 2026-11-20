using BuildingBlocks.Messaging.Events;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>Builds the event the Financial service turns into a Transportation StudentFee</summary>
    public static class BusFeeEventFactory
    {
        /// <param name="dueDate">Due date of the fee (when assigned); null keeps the current one</param>
        public static BusFeeIntegrationEvent From(BusAssignment assignment, BusFeeChangeType changeType, Guid changedBy, DateTime? dueDate = null)
            => new()
            {
                ChangeType = changeType,
                BusAssignmentId = assignment.Id,
                StudentId = assignment.StudentId,
                AcademicYearId = assignment.AcademicYearId,
                Amount = assignment.Fee.Amount,
                Currency = assignment.Fee.Currency,
                DueDate = dueDate,
                ChangedBy = changedBy
            };
    }
}
