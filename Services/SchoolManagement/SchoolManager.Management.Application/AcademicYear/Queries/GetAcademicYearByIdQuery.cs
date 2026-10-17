using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAcademicYearByIdQuery(Guid Id) : ICommand<AcademicYearDto?>;
}
