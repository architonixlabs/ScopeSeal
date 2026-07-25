using System.Text;
using ScopeSeal.Documents.Services;

namespace ScopeSeal.Infrastructure.Security;

public sealed class ContentTypeValidator : IContentTypeValidator
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/webp",
        "text/plain",
        "text/csv",
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
        "audio/mp4",
        "audio/x-m4a"
    ];

    private static readonly HashSet<string> BlockedExtensions =
    [
        ".exe", ".bat", ".cmd", ".com", ".msi", ".dll", ".scr", ".ps1",
        ".sh", ".html", ".htm", ".js", ".jsx", ".php", ".asp", ".aspx",
        ".jar", ".vbs", ".wsf", ".svg", ".zip", ".rar", ".7z"
    ];

    public ContentValidationResult ValidateDeclaredType(string declaredContentType, string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return new ContentValidationResult(false, "Content type is required.");
        }

        var normalized = declaredContentType.Trim().ToLowerInvariant();
        if (!AllowedContentTypes.Contains(normalized))
        {
            return new ContentValidationResult(false, $"Content type '{declaredContentType}' is not allowed.");
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (BlockedExtensions.Contains(extension))
        {
            return new ContentValidationResult(false, $"File extension '{extension}' is not allowed.");
        }

        return new ContentValidationResult(true, null);
    }

    public ContentValidationResult ValidateContent(ReadOnlySpan<byte> header, string declaredContentType)
    {
        var normalized = declaredContentType.Trim().ToLowerInvariant();

        return normalized switch
        {
            "application/pdf" when header.Length >= 4 &&
                header[0] == (byte)'%' && header[1] == (byte)'P' && header[2] == (byte)'D' && header[3] == (byte)'F' =>
                new ContentValidationResult(true, null),
            "image/png" when header.Length >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A =>
                new ContentValidationResult(true, null),
            "image/jpeg" when header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF =>
                new ContentValidationResult(true, null),
            "image/webp" when header.Length >= 12 &&
                header[0..4].SequenceEqual("RIFF"u8) &&
                header[8..12].SequenceEqual("WEBP"u8) =>
                new ContentValidationResult(true, null),
            "text/plain" or "text/csv" when IsSafeText(header) =>
                new ContentValidationResult(true, null),
            "audio/mpeg" when header.Length >= 3 &&
                ((header[0] == 0xFF && (header[1] & 0xE0) == 0xE0) ||
                 (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')) =>
                new ContentValidationResult(true, null),
            "audio/wav" when header.Length >= 12 &&
                header[0..4].SequenceEqual("RIFF"u8) &&
                header[8..12].SequenceEqual("WAVE"u8) =>
                new ContentValidationResult(true, null),
            "audio/ogg" when header.Length >= 4 &&
                header[0] == (byte)'O' && header[1] == (byte)'g' && header[2] == (byte)'g' && header[3] == (byte)'S' =>
                new ContentValidationResult(true, null),
            "audio/mp4" or "audio/x-m4a" when header.Length >= 8 &&
                (header[4..8].SequenceEqual("ftyp"u8)) =>
                new ContentValidationResult(true, null),
            _ => new ContentValidationResult(false, "File content does not match the declared content type.")
        };
    }

    private static bool IsSafeText(ReadOnlySpan<byte> header)
    {
        if (header.IsEmpty)
        {
            return true;
        }

        var sampleLength = Math.Min(header.Length, 512);
        for (var i = 0; i < sampleLength; i++)
        {
            var b = header[i];
            if (b == 0)
            {
                return false;
            }

            if (b < 0x09 || (b > 0x0D && b < 0x20 && b != 0x1B))
            {
                return false;
            }
        }

        return true;
    }
}
