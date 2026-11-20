using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Profile picture of a user (chat avatars in the teacher / parent apps); null when there is none.</summary>
    public record GetUserPhotoQuery(Guid UserId) : IQuery<UserPhotoDto?>;

    /// <summary>The picture's bytes and their content type (from the file signature)</summary>
    public record UserPhotoDto(byte[] Bytes, string ContentType);
}
