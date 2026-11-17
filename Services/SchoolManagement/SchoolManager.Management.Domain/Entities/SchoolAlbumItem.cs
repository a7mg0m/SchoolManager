using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    /// <summary>
    /// One file of the school album. The file itself is stored on disk (API wwwroot/upload/school-album);
    /// the database keeps its URL and details.
    /// </summary>
    public sealed class SchoolAlbumItem : BaseEntity
    {
        public Guid SchoolInfoId { get; private set; }

        /// <summary>Path of the file on the server, e.g. "/upload/school-album/3f2a….jpg"</summary>
        public string Url { get; private set; }

        /// <summary>Name of the file as uploaded (for display)</summary>
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        /// <summary>Size in bytes</summary>
        public long Size { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private SchoolAlbumItem() { }

        public SchoolAlbumItem(Guid schoolInfoId, string url, string fileName, string contentType, long size, AuditInfo auditInfo)
        {
            SchoolInfoId = schoolInfoId;
            Url = url;
            FileName = fileName;
            ContentType = contentType;
            Size = size;
            AuditInfo = auditInfo;
        }
    }
}
