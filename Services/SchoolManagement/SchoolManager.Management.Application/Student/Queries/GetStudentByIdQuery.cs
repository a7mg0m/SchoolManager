using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetStudentByIdQuery(Guid Id) : ICommand<StudentDto?>;
}
