using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>The student's home location (map pin); both values, or neither to clear it. False: student not found.</summary>
    public record SetStudentLocationCommand(Guid StudentId, double? Latitude, double? Longitude) : ICommand<bool>;
}
