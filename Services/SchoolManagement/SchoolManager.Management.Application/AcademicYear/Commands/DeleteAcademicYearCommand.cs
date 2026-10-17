using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteAcademicYearCommand(Guid Id) : ICommand<bool>;
}
