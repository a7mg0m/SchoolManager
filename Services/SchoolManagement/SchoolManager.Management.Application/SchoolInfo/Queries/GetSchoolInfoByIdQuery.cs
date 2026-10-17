using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetSchoolInfoByIdQuery(Guid Id) : ICommand<SchoolInfoDto?>;
}
