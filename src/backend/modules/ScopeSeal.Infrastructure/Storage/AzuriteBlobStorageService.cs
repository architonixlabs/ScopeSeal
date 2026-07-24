using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using ScopeSeal.Documents.Services;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Storage;

public sealed class AzuriteBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly DocumentUploadOptions _uploadOptions;

    public AzuriteBlobStorageService(IOptions<ScopeSealOptions> options)
    {
        var storage = options.Value.Storage;
        var connectionString = storage.ConnectionString ?? "UseDevelopmentStorage=true";
        _client = new BlobServiceClient(connectionString);
        _uploadOptions = options.Value.DocumentUpload;
    }

    public async Task WriteAsync(
        BlobContainerKind container,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(container, blobPath, cancellationToken);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(container, blobPath, cancellationToken);
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task CopyAsync(
        BlobContainerKind sourceContainer,
        string sourcePath,
        BlobContainerKind destinationContainer,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var sourceClient = await GetBlobClientAsync(sourceContainer, sourcePath, cancellationToken);
        var destinationClient = await GetBlobClientAsync(destinationContainer, destinationPath, cancellationToken);
        await destinationClient.StartCopyFromUriAsync(sourceClient.Uri, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(container, blobPath, cancellationToken);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(container, blobPath, cancellationToken);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    private async Task<BlobClient> GetBlobClientAsync(
        BlobContainerKind container,
        string blobPath,
        CancellationToken cancellationToken)
    {
        var containerName = ResolveContainerName(container);
        var containerClient = _client.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        return containerClient.GetBlobClient(blobPath);
    }

    private string ResolveContainerName(BlobContainerKind container) => container switch
    {
        BlobContainerKind.Quarantine => _uploadOptions.QuarantineContainer,
        BlobContainerKind.Permanent => _uploadOptions.PermanentContainer,
        _ => throw new ArgumentOutOfRangeException(nameof(container), container, null)
    };
}
