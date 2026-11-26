using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>Marks a student current / graduated / transferred; false when the student does not exist</summary>
    public record SetStudentStatusCommand(Guid Id, StudentStatus Status, DateOnly? Date, string? Note) : ICommand<bool>;

    /// <summary>Body of PUT /api/student/set-status</summary>
    public class SetStudentStatusDto
    {
        public Guid Id { get; set; }
        public StudentStatus Status { get; set; }
        /// <summary>yyyy-MM-dd; today when empty</summary>
        public DateOnly? Date { get; set; }
        public string? Note { get; set; }
    }
}
