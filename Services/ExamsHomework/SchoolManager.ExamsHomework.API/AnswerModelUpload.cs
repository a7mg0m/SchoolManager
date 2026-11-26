using BuildingBlocks;
using Microsoft.AspNetCore.Mvc;

namespace SchoolManager.ExamsHomework.API
{
    /// <summary>
    /// Upload of an exam's answer model (نموذج الإجابة): a PDF or a picture (JPG, PNG, WEBP, GIF) up to 10 MB,
    /// checked by its first bytes (not only the file name). Saved under wwwroot/answer-models and served at
    /// /exam-files/answer-models/{name} (the gateway forwards /exam-files to this service).
    /// Answers { data: "/exam-files/answer-models/{name}" }.
    /// </summary>
    public static class AnswerModelUpload
    {
        public const string UrlPrefix = "/exam-files";
        public const string Folder = "answer-models";
        public const long MaxBytes = 10 * 1024 * 1024;

        /// <summary>Request limit: the file plus the multipart envelope</summary>
        public const long MaxRequestBytes = MaxBytes + 64 * 1024;

        private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ".pdf", [".jpg"] = ".jpg", [".jpeg"] = ".jpg", [".png"] = ".png", [".webp"] = ".webp", [".gif"] = ".gif"
        };

        /// <summary>wwwroot of the service (created when missing)</summary>
        public static string WebRoot(IWebHostEnvironment environment)
            => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");

        public static async Task<IActionResult> SaveAsync(IFormFile? file, IWebHostEnvironment environment, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return new BadRequestObjectResult(new ApiResponse(400, "FileRequired"));
            if (file.Length > MaxBytes)
                return new BadRequestObjectResult(new ApiResponse(400, "FileTooLarge"));
            if (!Extensions.TryGetValue(Path.GetExtension(file.FileName), out var extension))
                return new BadRequestObjectResult(new ApiResponse(400, "FileTypeNotAllowed"));

            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            var isPdf = LooksLikePdf(header.AsSpan(0, read));
            if (extension == ".pdf" ? !isPdf : !LooksLikeImage(header.AsSpan(0, read)))
                return new BadRequestObjectResult(new ApiResponse(400, "FileTypeNotAllowed"));

            stream.Position = 0;
            var directory = Path.Combine(WebRoot(environment), Folder);
            Directory.CreateDirectory(directory);
            var name = $"{Guid.NewGuid():N}{extension}";
            await using (var target = File.Create(Path.Combine(directory, name)))
            {
                await stream.CopyToAsync(target, cancellationToken);
            }
            return new OkObjectResult(new ApiResponse { message = "Uploaded Successfully", data = $"{UrlPrefix}/{Folder}/{name}" });
        }

        /// <summary>"%PDF"</summary>
        private static bool LooksLikePdf(ReadOnlySpan<byte> h) =>
            h.Length >= 4 && h[0] == 0x25 && h[1] == 0x50 && h[2] == 0x44 && h[3] == 0x46;

        /// <summary>JPEG / PNG / GIF / WEBP signatures</summary>
        private static bool LooksLikeImage(ReadOnlySpan<byte> h) =>
            (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
            || (h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47)
            || (h.Length >= 6 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38)
            || (h.Length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50);
    }
}
