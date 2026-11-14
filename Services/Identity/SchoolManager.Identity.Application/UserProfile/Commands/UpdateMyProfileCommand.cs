using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Updates the logged-in user's profile. The email is also saved on the User, and
    /// email / phone / address on the linked Employee (if any). Returns the updated profile.
    /// </summary>
    public record UpdateMyProfileCommand(
        string FirstName,
        string LastName,
        string Email,
        string? PhoneNumber,
        string? Address,
        byte[]? ProfileImage,
        bool RemoveProfileImage
    ) : ICommand<MyProfileDto>;
}
