using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Multipart form body used for event banner and profile picture uploads.</summary>
public class ImageUploadRequest
{
    /// <summary>The image file (JPEG, PNG, WebP or GIF, 5 MB maximum).</summary>
    [Required]
    public IFormFile? File { get; set; }
}
