using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetActiveVaccinesQuery() : ICommand<List<VaccineDto>>;
}
