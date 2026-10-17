using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record UpdateUserProfileCommand(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Address,
        byte[] ProfileImage,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
