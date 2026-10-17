using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllSchoolInfosQuery() : ICommand<List<SchoolInfoDto>>;
}
