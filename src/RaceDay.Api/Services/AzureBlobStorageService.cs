using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/> built on the Azure.Storage.Blobs SDK.
/// The same class talks to Azurite during development and in Docker, and to a real Azure Storage
/// account if the connection string is changed.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(BlobServiceClient serviceClient, IOptions<BlobStorageOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        _serviceClient = serviceClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(string containerName, Stream content, string contentType, string fileExtension, CancellationToken cancellationToken = default)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);

        // The container is created on first use, so the API also works against a brand-new, empty Azurite instance.
        await container.CreateIfNotExistsAsync(
            _options.AllowPublicRead ? PublicAccessType.Blob : PublicAccessType.None,
            cancellationToken: cancellationToken);

        // A GUID file name avoids collisions and stops users overwriting each other's images.
        var blobName = $"{Guid.NewGuid():N}{fileExtension}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
                CacheControl = "public, max-age=86400"
            }
        }, cancellationToken);

        _logger.LogInformation("Uploaded image {BlobName} to container {Container}.", blobName, containerName);
        return BuildPublicUrl(containerName, blobName, blob.Uri);
    }

    public async Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        // Blob URLs end in ".../{container}/{blobName}" for both Azure and Azurite, so the last two segments identify the blob.
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return;
        }

        var containerName = segments[^2];
        var blobName = Uri.UnescapeDataString(segments[^1]);

        // Only blobs in RaceDay's own containers may be removed.
        if (containerName != _options.EventBannerContainer && containerName != _options.ProfilePictureContainer)
        {
            return;
        }

        try
        {
            await _serviceClient.GetBlobContainerClient(containerName)
                .GetBlobClient(blobName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cleaning up an old image must never fail the user's request, so the error is only logged.
            _logger.LogWarning(ex, "Could not delete old image {ImageUrl}.", imageUrl);
        }
    }

    private string BuildPublicUrl(string containerName, string blobName, Uri sdkUri)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return sdkUri.ToString();
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{containerName}/{blobName}";
    }
}
