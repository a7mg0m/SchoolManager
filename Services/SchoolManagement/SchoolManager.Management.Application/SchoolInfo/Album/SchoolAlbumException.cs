namespace SchoolManager.Management.Application
{
    /// <summary>An album upload that cannot be saved; the controller returns <see cref="ErrorKey"/> (and the file).</summary>
    public class SchoolAlbumException : Exception
    {
        public SchoolAlbumException(string errorKey, string? fileName = null) : base(errorKey)
        {
            ErrorKey = errorKey;
            FileName = fileName;
        }

        public string ErrorKey { get; }

        /// <summary>The file that was rejected, if the error is about one file</summary>
        public string? FileName { get; }
    }
}
