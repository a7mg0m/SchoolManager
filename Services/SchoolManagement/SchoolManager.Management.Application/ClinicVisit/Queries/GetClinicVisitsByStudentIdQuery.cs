using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetClinicVisitsByStudentIdQuery(Guid StudentId) : ICommand<List<ClinicVisitDto>>;
}
