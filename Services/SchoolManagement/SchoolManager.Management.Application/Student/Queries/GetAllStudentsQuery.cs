using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllStudentsQuery() : ICommand<List<StudentDto>>;
}
