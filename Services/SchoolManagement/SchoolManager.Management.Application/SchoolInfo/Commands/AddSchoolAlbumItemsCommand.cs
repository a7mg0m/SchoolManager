using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// Saves the files in wwwroot/upload/school-album and adds their URLs to the album of the school info.
    /// Returns the new album items.
    /// </summary>
    public record AddSchoolAlbumItemsCommand(List<AlbumFileUpload> Files) : ICommand<List<SchoolAlbumItemDto>>;
}
