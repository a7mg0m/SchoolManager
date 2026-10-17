using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSchoolInfoCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        string? Address,
        string? Phone,
        string? Email,
        byte[]? Logo,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
