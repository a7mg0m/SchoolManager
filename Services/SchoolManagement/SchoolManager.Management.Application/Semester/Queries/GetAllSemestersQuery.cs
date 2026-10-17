using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllSemestersQuery() : ICommand<List<SemesterDto>>;
}
