using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Quick client-facing checks on an uploaded image before it is forwarded to the API.
/// These only exist to give fast feedback; the API repeats the checks (including the file signature).
/// </summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string AcceptAttribute = "image/jpeg,image/png,image/webp,image/gif";

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    /// <summary>Adds a model error to <paramref name="fieldName"/> when the file is not an acceptable image.</summary>
    public static bool Validate(IFormFile? file, ModelStateDictionary modelState, string fieldName)
    {
        if (file == null || file.Length == 0)
        {
            modelState.AddModelError(fieldName, "Choose an image to upload.");
            return false;
        }

        if (file.Length > MaxBytes)
        {
            modelState.AddModelError(fieldName, "Images must be 5 MB or smaller.");
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            modelState.AddModelError(fieldName, "Use a JPG, PNG, WebP or GIF image.");
            return false;
        }

        return true;
    }
}
