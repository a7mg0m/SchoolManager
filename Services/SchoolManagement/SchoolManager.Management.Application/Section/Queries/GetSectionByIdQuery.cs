using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetSectionByIdQuery(Guid Id) : ICommand<SectionDto?>;
}
