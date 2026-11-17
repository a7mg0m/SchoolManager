namespace SchoolManager.Management.Application
{
    /// <summary>A file sent to the album upload (the API reads it from the multipart request).</summary>
    public sealed record AlbumFileUpload(string FileName, long Length, Stream Content);
}
