namespace RaceDay.Api.Services;

/// <summary>
/// Abstraction over Azure Blob Storage. Controllers depend on this interface rather than the Azure SDK
/// directly, which keeps them testable (unit tests use an in-memory fake) and keeps all storage
/// details in one place.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Uploads an image to the given container and returns the public URL that should be saved in the database.</summary>
    Task<string> UploadImageAsync(string containerName, Stream content, string contentType, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously uploaded image identified by its stored URL. Missing blobs are ignored.</summary>
    Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default);
}
