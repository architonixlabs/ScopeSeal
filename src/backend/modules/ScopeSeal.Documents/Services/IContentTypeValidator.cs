namespace ScopeSeal.Documents.Services;

public sealed record ContentValidationResult(bool IsValid, string? Error);

public interface IContentTypeValidator
{
    ContentValidationResult ValidateDeclaredType(string declaredContentType, string originalFileName);

    ContentValidationResult ValidateContent(ReadOnlySpan<byte> header, string declaredContentType);
}
