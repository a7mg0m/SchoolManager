namespace SchoolManager.Notification.Domain
{
    public sealed record Attachment
    {
        public string FileName { get; }
        public string Url { get; }
        public string ContentType { get; }
        public long SizeInBytes { get; }

        private Attachment()
        {
            FileName = string.Empty;
            Url = string.Empty;
            ContentType = string.Empty;
        }

        public Attachment(string fileName, string url, string contentType, long sizeInBytes)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ChatDomainException("Attachment file name is required.");
            if (string.IsNullOrWhiteSpace(url)) throw new ChatDomainException("Attachment url is required.");
            if (sizeInBytes < 0) throw new ChatDomainException("Attachment size is invalid.");

            FileName = fileName;
            Url = url;
            ContentType = contentType;
            SizeInBytes = sizeInBytes;
        }
    }
}
