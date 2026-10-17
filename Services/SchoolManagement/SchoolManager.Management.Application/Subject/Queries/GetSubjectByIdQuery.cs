using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetSubjectByIdQuery(Guid Id) : ICommand<SubjectDto?>;
}
