namespace SchoolManager.Notification.Application
{
    /// <summary>What a chat attachment is (from its file type)</summary>
    public enum ChatMediaKind
    {
        Image,
        Video,
        Audio
    }

    /// <summary>A stored chat file: where it is on disk and how to serve it</summary>
    public record ChatStoredFile(string PhysicalPath, string ContentType);

    /// <summary>
    /// Pictures, videos and voice notes of the chats. They are private: kept outside wwwroot (ChatFiles/{roomId}/…)
    /// and only served to the room's participants by GET /api/chat/files/{roomId}/{name}.
    /// </summary>
    public interface IChatFileStore
    {
        /// <summary>Saves the file under a new unique name in the room's folder; returns that name (e.g. "3f2a….m4a")</summary>
        Task<string> SaveAsync(Guid roomId, Stream content, string extension, CancellationToken cancellationToken = default);

        /// <summary>The room's file, or null when the name is not one of ours / the file is gone</summary>
        ChatStoredFile? Find(Guid roomId, string fileName);
    }

    /// <summary>The chat media rules shared by the upload, the send validation, the list preview and the push text</summary>
    public static class ChatMedia
    {
        /// <summary>Relative URL of a chat file (through the gateway)</summary>
        public const string UrlPrefix = "/api/chat/files";

        public const long MaxImageBytes = 10 * 1024 * 1024;
        public const long MaxAudioBytes = 10 * 1024 * 1024;

        /// <summary>Short videos: the IIS request limit is ~28 MB</summary>
        public const long MaxVideoBytes = 25 * 1024 * 1024;

        public const int MaxAttachmentsPerMessage = 10;

        /// <summary>Allowed extensions → kind and the content type the file is served with</summary>
        public static readonly IReadOnlyDictionary<string, (ChatMediaKind Kind, string ContentType)> Types =
            new Dictionary<string, (ChatMediaKind, string)>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = (ChatMediaKind.Image, "image/jpeg"),
                [".jpeg"] = (ChatMediaKind.Image, "image/jpeg"),
                [".png"] = (ChatMediaKind.Image, "image/png"),
                [".webp"] = (ChatMediaKind.Image, "image/webp"),
                [".gif"] = (ChatMediaKind.Image, "image/gif"),
                [".heic"] = (ChatMediaKind.Image, "image/heic"),
                [".mp4"] = (ChatMediaKind.Video, "video/mp4"),
                [".mov"] = (ChatMediaKind.Video, "video/quicktime"),
                [".3gp"] = (ChatMediaKind.Video, "video/3gpp"),
                [".webm"] = (ChatMediaKind.Video, "video/webm"),
                [".m4a"] = (ChatMediaKind.Audio, "audio/mp4"),
                [".aac"] = (ChatMediaKind.Audio, "audio/aac"),
                [".mp3"] = (ChatMediaKind.Audio, "audio/mpeg"),
                [".wav"] = (ChatMediaKind.Audio, "audio/wav"),
                [".ogg"] = (ChatMediaKind.Audio, "audio/ogg"),
                [".opus"] = (ChatMediaKind.Audio, "audio/ogg"),
                // Voice notes recorded in a browser (Chrome: Opus in WebM)
                [".weba"] = (ChatMediaKind.Audio, "audio/webm"),
            };

        public static long MaxBytes(ChatMediaKind kind) => kind switch
        {
            ChatMediaKind.Video => MaxVideoBytes,
            ChatMediaKind.Audio => MaxAudioBytes,
            _ => MaxImageBytes
        };

        public static string Url(Guid roomId, string fileName) => $"{UrlPrefix}/{roomId}/{fileName}";

        /// <summary>True for a file URL of this room (a message may only point at its own room's uploads)</summary>
        public static bool IsRoomFileUrl(string? url, Guid roomId) =>
            url is not null
            && url.StartsWith($"{UrlPrefix}/{roomId}/", StringComparison.OrdinalIgnoreCase)
            && url.IndexOf("..", StringComparison.Ordinal) < 0;

        /// <summary>"image" | "video" | "audio" | "file" from a content type (for the chat list preview)</summary>
        public static string KindName(string? contentType) => contentType switch
        {
            not null when contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => "image",
            not null when contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) => "video",
            not null when contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) => "audio",
            _ => "file"
        };

        /// <summary>Push text of a message: its words, else what it carries ("🎤 رسالة صوتية")</summary>
        public static string PreviewAr(string? content, string? firstAttachmentContentType)
        {
            if (!string.IsNullOrWhiteSpace(content)) return content;
            return KindName(firstAttachmentContentType) switch
            {
                "image" => "📷 صورة",
                "video" => "🎥 فيديو",
                "audio" => "🎤 رسالة صوتية",
                _ => "📎 ملف"
            };
        }

        /// <summary>True when [offset..] of the header holds these bytes</summary>
        private static bool At(ReadOnlySpan<byte> h, int offset, params byte[] bytes)
        {
            if (h.Length < offset + bytes.Length) return false;
            for (var i = 0; i < bytes.Length; i++)
                if (h[offset + i] != bytes[i]) return false;
            return true;
        }

        /// <summary>
        /// The first bytes really are such a file (an uploaded page or program renamed to .jpg is refused).
        /// WebM / Matroska is checked for both video and browser voice notes.
        /// </summary>
        public static bool LooksLike(ChatMediaKind kind, string extension, ReadOnlySpan<byte> h)
        {
            var isoMedia = At(h, 4, 0x66, 0x74, 0x79, 0x70); // "ftyp": MP4 / MOV / 3GP / M4A / HEIC
            var webm = At(h, 0, 0x1A, 0x45, 0xDF, 0xA3);
            switch (kind)
            {
                case ChatMediaKind.Image:
                    return At(h, 0, 0xFF, 0xD8, 0xFF)                                    // JPEG
                        || At(h, 0, 0x89, 0x50, 0x4E, 0x47)                              // PNG
                        || At(h, 0, 0x47, 0x49, 0x46, 0x38)                              // GIF
                        || (At(h, 0, 0x52, 0x49, 0x46, 0x46) && At(h, 8, 0x57, 0x45, 0x42, 0x50)) // WEBP
                        || (isoMedia && extension.Equals(".heic", StringComparison.OrdinalIgnoreCase));
                case ChatMediaKind.Video:
                    return isoMedia || webm;
                default:
                    return isoMedia || webm
                        || At(h, 0, 0x49, 0x44, 0x33)                                    // MP3 with ID3
                        || (h.Length >= 2 && h[0] == 0xFF && (h[1] & 0xE0) == 0xE0)   // MP3 / AAC frame
                        || At(h, 0, 0x4F, 0x67, 0x67, 0x53)                              // OGG
                        || (At(h, 0, 0x52, 0x49, 0x46, 0x46) && At(h, 8, 0x57, 0x41, 0x56, 0x45)); // WAV
            }
        }
    }
}
