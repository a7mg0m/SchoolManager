namespace SchoolManager.Management.Application
{
    /// <summary>What the school album accepts (images and videos). Error keys are returned to the admin page.</summary>
    public static class SchoolAlbumRules
    {
        /// <summary>Folder under wwwroot</summary>
        public const string Folder = "upload/school-album";

        public const long MaxImageBytes = 10 * 1024 * 1024;   // 10 MB
        public const long MaxVideoBytes = 100 * 1024 * 1024;  // 100 MB

        /// <summary>Largest upload request (one video plus form overhead); the servers' body limits are set to this</summary>
        public const long MaxRequestBytes = 110 * 1024 * 1024;

        public const int MaxFilesPerUpload = 20;

        public const string SchoolInfoRequired = "SchoolInfoRequired";
        public const string NoFiles = "NoFiles";
        public const string TooManyFiles = "TooManyFiles";
        public const string FileTooLarge = "FileTooLarge";
        public const string FileTypeNotAllowed = "FileTypeNotAllowed";

        /// <summary>Allowed extensions and the content type stored for them</summary>
        public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            // Videos
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".mov"] = "video/quicktime"
        };

        public static bool IsVideo(string contentType) => contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        /// <summary>Size limit for a content type (videos may be larger than images)</summary>
        public static long MaxBytesFor(string contentType) => IsVideo(contentType) ? MaxVideoBytes : MaxImageBytes;

        /// <summary>
        /// True when the first bytes really are a file of that extension
        /// (a renamed file, e.g. a script called "photo.jpg", is rejected).
        /// </summary>
        public static bool HasValidSignature(ReadOnlySpan<byte> head, string extension)
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
                // MP4 / MOV (ISO base media): a box size, then "ftyp" at bytes 4..8
                ".mp4" or ".mov" => head.Length >= 8 && head[4..8].SequenceEqual("ftyp"u8),
                // WebM (Matroska / EBML)
                ".webm" => StartsWith(head, 0x1A, 0x45, 0xDF, 0xA3),
                _ => false
            };
        }
    }
}
