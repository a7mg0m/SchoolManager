using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetSemesterByIdQuery(Guid Id) : ICommand<SemesterDto?>;
}
