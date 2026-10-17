using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record CreateUserProfileCommand(
        Guid UserId,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Address,
        byte[] ProfileImage,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
