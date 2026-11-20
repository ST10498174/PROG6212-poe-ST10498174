using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// View and update the logged-in user's own profile and profile picture.
/// Available to both roles; the API identifies the user from the session, so nobody can edit another person's profile.
/// </summary>
[RoleAuthorize]
public class ProfileController : RaceDayControllerBase
{
    public ProfileController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await Api.GetProfileAsync();
        if (!profile.Success || profile.Data == null)
        {
            return ApiFailure(profile);
        }

        HttpContext.Session.UpdateDisplayDetails(profile.Data);
        return View(ToViewModel(profile.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth can't be in the future.");
        }

        if (!ModelState.IsValid)
        {
            await RestoreReadOnlyFieldsAsync(model);
            return View(model);
        }

        var result = await Api.UpdateProfileAsync(new ProfileUpdateRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth!.Value.Date
        });

        if (!result.Success)
        {
            AddApiErrors(result);
            await RestoreReadOnlyFieldsAsync(model);
            return View(model);
        }

        await RefreshSessionAsync();
        Success("Profile saved.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sends the picture to the API, which stores it in Blob Storage and saves the URL.</summary>
    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (!ImageUploadRules.Validate(picture, ModelState, "picture"))
        {
            Error(ModelState["picture"]!.Errors[0].ErrorMessage);
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture!);
        if (!result.Success)
        {
            Error(result.ErrorMessage ?? "The picture couldn't be uploaded.");
            return RedirectToAction(nameof(Index));
        }

        await RefreshSessionAsync();
        Success("Profile picture updated.");
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(ProfileDto profile) => new()
    {
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        DateOfBirth = profile.DateOfBirth,
        Role = profile.Role,
        ProfilePictureUrl = profile.ProfilePictureUrl
    };

    // Role and picture are not posted with the form, so they are reloaded when the form is redisplayed.
    private async Task RestoreReadOnlyFieldsAsync(ProfileViewModel model)
    {
        var profile = await Api.GetProfileAsync();
        model.Role = profile.Data?.Role ?? CurrentUser.Role;
        model.ProfilePictureUrl = profile.Data?.ProfilePictureUrl;
    }

    private async Task RefreshSessionAsync()
    {
        var profile = await Api.GetProfileAsync();
        if (profile.Success && profile.Data != null)
        {
            HttpContext.Session.UpdateDisplayDetails(profile.Data);
        }
    }
}
