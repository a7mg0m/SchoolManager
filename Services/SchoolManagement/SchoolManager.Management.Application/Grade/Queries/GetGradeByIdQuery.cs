using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetGradeByIdQuery(Guid Id) : ICommand<GradeDto?>;
}
