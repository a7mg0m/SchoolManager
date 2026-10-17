using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateParentCommand(
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
