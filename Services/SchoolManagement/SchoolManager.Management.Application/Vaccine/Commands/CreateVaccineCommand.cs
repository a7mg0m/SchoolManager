using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record CreateVaccineCommand(
        string NameAr,
        string NameEn,
        int TotalDoses,
        bool IsMandatory
    ) : ICommand<Guid>;
}
