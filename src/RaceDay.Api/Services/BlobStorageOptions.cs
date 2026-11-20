namespace RaceDay.Api.Services;

/// <summary>
/// Strongly typed settings for the "BlobStorage" section of appsettings.json.
/// Locally and in Docker these settings point at Azurite, the Azure Storage emulator,
/// so the exact same Azure SDK code works against the emulator and a real storage account.
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>Azure Storage connection string. "UseDevelopmentStorage=true" targets a local Azurite instance.</summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    /// <summary>Container that stores Organiser event banner images.</summary>
    public string EventBannerContainer { get; set; } = "event-banners";

    /// <summary>Container that stores user profile pictures.</summary>
    public string ProfilePictureContainer { get; set; } = "profile-pictures";

    /// <summary>
    /// Optional browser-facing base URL for blobs (for example http://localhost:10000/devstoreaccount1).
    /// Inside Docker the API reaches Azurite at http://azurite:10000, but a browser on the host cannot
    /// resolve that container name, so the URL saved in the database uses this public address instead.
    /// When empty, the URL returned by the Azure SDK is stored as-is.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Creates containers with anonymous blob read access so images can be shown in an &lt;img&gt; tag.
    /// Azurite supports this. Production accounts that disable public access should set this to false
    /// and serve images through SAS URLs instead.
    /// </summary>
    public bool AllowPublicRead { get; set; } = true;
}
