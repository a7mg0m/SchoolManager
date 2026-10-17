using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllSubjectsQuery() : ICommand<List<SubjectDto>>;
}
