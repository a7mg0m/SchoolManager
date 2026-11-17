using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>Removes an album item and deletes its file. False when it does not exist.</summary>
    public record DeleteSchoolAlbumItemCommand(Guid Id) : ICommand<bool>;
}
