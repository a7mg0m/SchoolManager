using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllTeachersQuery() : ICommand<List<TeacherDto>>;
}
