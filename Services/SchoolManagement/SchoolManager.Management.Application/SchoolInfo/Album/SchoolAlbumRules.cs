namespace SchoolManager.Management.Application
{
    /// <summary>What the school album accepts. Error keys are returned to the admin page.</summary>
    public static class SchoolAlbumRules
    {
        /// <summary>Folder under wwwroot</summary>
        public const string Folder = "upload/school-album";

        public const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB
        public const int MaxFilesPerUpload = 20;

        public const string SchoolInfoRequired = "SchoolInfoRequired";
        public const string NoFiles = "NoFiles";
        public const string TooManyFiles = "TooManyFiles";
        public const string FileTooLarge = "FileTooLarge";
        public const string FileTypeNotAllowed = "FileTypeNotAllowed";

        /// <summary>Allowed extensions and the content type stored for them</summary>
        public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        /// <summary>
        /// True when the first bytes are really an image of that extension
        /// (a renamed file, e.g. a script called "photo.jpg", is rejected).
        /// </summary>
        public static bool HasImageSignature(ReadOnlySpan<byte> head, string extension)
        {
            static bool StartsWith(ReadOnlySpan<byte> data, params byte[] signature)
                => data.Length >= signature.Length && data[..signature.Length].SequenceEqual(signature);

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => StartsWith(head, 0xFF, 0xD8, 0xFF),
                ".png" => StartsWith(head, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
                ".gif" => StartsWith(head, 0x47, 0x49, 0x46, 0x38), // GIF8
                ".webp" => head.Length >= 12 && StartsWith(head, 0x52, 0x49, 0x46, 0x46) // RIFF....WEBP
                           && head[8..12].SequenceEqual("WEBP"u8),
                _ => false
            };
        }
    }
}
