using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetClinicVisitByIdQuery(Guid Id) : ICommand<ClinicVisitDto?>;
}
