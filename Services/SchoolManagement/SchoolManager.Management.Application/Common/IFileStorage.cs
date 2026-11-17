namespace SchoolManager.Management.Application
{
    /// <summary>Stores uploaded files where the API can serve them (wwwroot).</summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Saves the content under a new unique name in <paramref name="folder"/> (e.g. "upload/school-album")
        /// and returns its URL path, e.g. "/upload/school-album/3f2a….jpg".
        /// </summary>
        Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken = default);

        /// <summary>Deletes the file of a URL returned by <see cref="SaveAsync"/>; nothing happens when it is missing.</summary>
        void Delete(string url);
    }
}
