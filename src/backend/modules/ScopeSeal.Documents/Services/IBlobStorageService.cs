namespace ScopeSeal.Documents.Services;

public enum BlobContainerKind
{
    Quarantine = 0,
    Permanent = 1
}

public interface IBlobStorageService
{
    Task WriteAsync(
        BlobContainerKind container,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default);

    Task CopyAsync(
        BlobContainerKind sourceContainer,
        string sourcePath,
        BlobContainerKind destinationContainer,
        string destinationPath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default);
}
