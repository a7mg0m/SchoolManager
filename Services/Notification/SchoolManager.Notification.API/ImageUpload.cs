using BuildingBlocks;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API
{
    /// <summary>
    /// Upload of a notification / event picture: JPG, PNG, WEBP or GIF up to 5 MB, checked by its first bytes
    /// (not only the file name). Answers { data: "/notification-files/{folder}/{name}" }.
    /// </summary>
    public static class ImageUpload
    {
        public const long MaxBytes = 5 * 1024 * 1024;

        /// <summary>Request limit: the picture plus the multipart envelope</summary>
        public const long MaxRequestBytes = MaxBytes + 64 * 1024;

        private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ".jpg", [".jpeg"] = ".jpg", [".png"] = ".png", [".webp"] = ".webp", [".gif"] = ".gif"
        };

        public static async Task<IActionResult> SaveAsync(IFormFile? file, ImageFolder folder, IImageFileStore store, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return new BadRequestObjectResult(new ApiResponse(400, "ImageRequired"));
            if (file.Length > MaxBytes)
                return new BadRequestObjectResult(new ApiResponse(400, "ImageTooLarge"));
            if (!Extensions.TryGetValue(Path.GetExtension(file.FileName), out var extension))
                return new BadRequestObjectResult(new ApiResponse(400, "ImageTypeNotAllowed"));

            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            if (!LooksLikeImage(header.AsSpan(0, read)))
                return new BadRequestObjectResult(new ApiResponse(400, "ImageTypeNotAllowed"));

            stream.Position = 0;
            var url = await store.SaveAsync(stream, extension, folder, cancellationToken);
            return new OkObjectResult(new ApiResponse { message = "Uploaded Successfully", data = url });
        }

        /// <summary>JPEG / PNG / GIF / WEBP signatures</summary>
        private static bool LooksLikeImage(ReadOnlySpan<byte> h) =>
            (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
            || (h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47)
            || (h.Length >= 6 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38)
            || (h.Length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50);
    }
}
