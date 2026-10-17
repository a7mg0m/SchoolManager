using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetTeacherByIdQuery(Guid Id) : ICommand<TeacherDto?>;
}
