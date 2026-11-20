using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetVaccineByIdQuery(Guid Id) : ICommand<VaccineDto?>;
}
