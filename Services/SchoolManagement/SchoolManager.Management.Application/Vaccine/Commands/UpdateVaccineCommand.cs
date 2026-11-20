using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record UpdateVaccineCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        int TotalDoses,
        bool IsMandatory
    ) : ICommand<bool>;
}
