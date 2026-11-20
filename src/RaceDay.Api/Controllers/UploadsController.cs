using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Handles image uploads for Part 3. The MVC application never talks to Azure directly:
/// it posts the file to these endpoints, the API stores it in Azure Blob Storage (Azurite locally)
/// and saves the resulting blob URL in the database.
/// </summary>
[Route("api")]
public class UploadsController : ApiControllerBase
{
    /// <summary>Largest image the API accepts (5 MB).</summary>
    public const long MaxImageBytes = 5 * 1024 * 1024;

    // Allowed MIME types mapped to the file extension used for the stored blob.
    private static readonly Dictionary<string, string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(RaceDayDbContext db, IBlobStorageService blobStorage, IOptions<BlobStorageOptions> options, ILogger<UploadsController> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Uploads or replaces the banner image of an event owned by the logged-in Organiser.</summary>
    /// <remarks>Send a multipart/form-data request with a single "file" field. Returns the stored blob URL.</remarks>
    /// <response code="200">Banner stored in Blob Storage and URL saved against the event.</response>
    /// <response code="400">No file, file too large, or not a supported image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="503">Blob Storage could not be reached.</response>
    [HttpPost("events/{eventId:int}/banner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadEventBanner(int eventId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(new object[] { eventId }, cancellationToken);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only upload banners for your own events." });

        var validation = await ValidateImageAsync(request.File, cancellationToken);
        if (validation.Error != null) return validation.Error;

        var upload = await TryUploadAsync(_options.EventBannerContainer, request.File!, validation.Extension!, cancellationToken);
        if (upload.Error != null) return upload.Error;

        var previousUrl = eventItem.BannerImageUrl;
        eventItem.BannerImageUrl = upload.Url;
        await _db.SaveChangesAsync(cancellationToken);

        // The replaced banner is removed only after the new URL has been saved successfully.
        await _blobStorage.DeleteImageAsync(previousUrl, cancellationToken);

        return Ok(new { eventItem.EventId, eventItem.BannerImageUrl });
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture.</summary>
    /// <remarks>Send a multipart/form-data request with a single "file" field. Available to both roles.</remarks>
    /// <response code="200">Picture stored in Blob Storage and URL saved against the user.</response>
    /// <response code="400">No file, file too large, or not a supported image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Blob Storage could not be reached.</response>
    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var accessError = RequireAuthenticated();
        if (accessError != null) return accessError;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId, cancellationToken);
        if (user == null) return NotFound(new { message = "User profile was not found." });

        var validation = await ValidateImageAsync(request.File, cancellationToken);
        if (validation.Error != null) return validation.Error;

        var upload = await TryUploadAsync(_options.ProfilePictureContainer, request.File!, validation.Extension!, cancellationToken);
        if (upload.Error != null) return upload.Error;

        var previousUrl = user.ProfilePictureUrl;
        user.ProfilePictureUrl = upload.Url;
        await _db.SaveChangesAsync(cancellationToken);
        await _blobStorage.DeleteImageAsync(previousUrl, cancellationToken);

        return Ok(new { user.UserId, user.ProfilePictureUrl });
    }

    /// <summary>
    /// Checks the size, declared MIME type and the file's leading "magic bytes" so that a renamed
    /// non-image file cannot be stored as a picture.
    /// </summary>
    private async Task<(IActionResult? Error, string? Extension)> ValidateImageAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return (BadRequest(new { message = "Please choose an image file to upload." }), null);
        }

        if (file.Length > MaxImageBytes)
        {
            return (BadRequest(new { message = "Images must be 5 MB or smaller." }), null);
        }

        if (!AllowedImageTypes.TryGetValue(file.ContentType ?? string.Empty, out var extension))
        {
            return (BadRequest(new { message = "Only JPEG, PNG, WebP or GIF images are supported." }), null);
        }

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = 0;
            while (read < header.Length)
            {
                var count = await stream.ReadAsync(header.AsMemory(read), cancellationToken);
                if (count == 0) break;
                read += count;
            }
        }

        if (!LooksLikeImage(header))
        {
            return (BadRequest(new { message = "The uploaded file is not a valid image." }), null);
        }

        return (null, extension);
    }

    private static bool LooksLikeImage(byte[] h)
    {
        var isJpeg = h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
        var isPng = h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
        var isGif = h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38;
        var isWebp = h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                     && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;
        return isJpeg || isPng || isGif || isWebp;
    }

    /// <summary>Uploads the file and converts storage failures into a 503 response instead of an unhandled exception.</summary>
    private async Task<(IActionResult? Error, string? Url)> TryUploadAsync(string container, IFormFile file, string extension, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _blobStorage.UploadImageAsync(container, stream, file.ContentType, extension, cancellationToken);
            return (null, url);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Image upload to container {Container} failed.", container);
            return (StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is unavailable right now. Please try again shortly." }), null);
        }
    }
}
