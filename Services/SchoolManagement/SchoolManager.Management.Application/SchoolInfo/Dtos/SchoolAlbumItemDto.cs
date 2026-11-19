namespace SchoolManager.Management.Application
{
    /// <summary>A file of the school album</summary>
    public class SchoolAlbumItemDto
    {
        public Guid Id { get; set; }
        /// <summary>URL path of the file, e.g. "/upload/school-album/3f2a….jpg" (served through the gateway)</summary>
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        /// <summary>"image" or "video" (from the content type)</summary>
        public string MediaType => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "image";
        /// <summary>Size in bytes</summary>
        public long Size { get; set; }
        public DateTime UploadedOn { get; set; }
    }
}
