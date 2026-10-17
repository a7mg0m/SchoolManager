using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSchoolInfoCommand(
        string NameAr,
        string NameEn,
        string? Address,
        string? Phone,
        string? Email,
        byte[]? Logo,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
