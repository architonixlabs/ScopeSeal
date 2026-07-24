using System.Collections.Concurrent;
using ScopeSeal.Documents.Services;

namespace ScopeSeal.Infrastructure.Storage;

public sealed class InMemoryBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, StoredBlob> _blobs = new();

    public Task WriteAsync(
        BlobContainerKind container,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        content.CopyTo(memory);
        _blobs[BuildKey(container, blobPath)] = new StoredBlob(memory.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        if (!_blobs.TryGetValue(BuildKey(container, blobPath), out var blob))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(blob.Content, writable: false));
    }

    public Task CopyAsync(
        BlobContainerKind sourceContainer,
        string sourcePath,
        BlobContainerKind destinationContainer,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var sourceKey = BuildKey(sourceContainer, sourcePath);
        if (!_blobs.TryGetValue(sourceKey, out var source))
        {
            throw new InvalidOperationException($"Source blob '{sourcePath}' was not found.");
        }

        _blobs[BuildKey(destinationContainer, destinationPath)] = source with { };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(BuildKey(container, blobPath), out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_blobs.ContainsKey(BuildKey(container, blobPath)));

    private static string BuildKey(BlobContainerKind container, string blobPath) =>
        $"{container}:{blobPath}";

    private sealed record StoredBlob(byte[] Content, string ContentType);
}
