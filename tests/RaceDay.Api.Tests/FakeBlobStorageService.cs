using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// In-memory stand-in for Azure Blob Storage so upload logic can be unit tested without Azurite running.
/// It records every upload and delete so tests can assert on them.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public List<(string Container, string Url, string ContentType)> Uploads { get; } = new();
    public List<string> DeletedUrls { get; } = new();
    public bool ThrowOnUpload { get; set; }

    public Task<string> UploadImageAsync(string containerName, Stream content, string contentType, string fileExtension, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUpload)
        {
            throw new InvalidOperationException("Simulated storage outage.");
        }

        var url = $"http://127.0.0.1:10000/devstoreaccount1/{containerName}/{Guid.NewGuid():N}{fileExtension}";
        Uploads.Add((containerName, url, contentType));
        return Task.FromResult(url);
    }

    public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            DeletedUrls.Add(imageUrl);
        }

        return Task.CompletedTask;
    }
}
