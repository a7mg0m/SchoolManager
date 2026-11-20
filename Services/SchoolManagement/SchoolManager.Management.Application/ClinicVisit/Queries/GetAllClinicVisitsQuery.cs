using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllClinicVisitsQuery() : ICommand<List<ClinicVisitDto>>;
}
