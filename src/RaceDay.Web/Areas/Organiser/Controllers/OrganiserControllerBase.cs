using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Controllers;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>
/// Base class for every Organiser page. The [RoleAuthorize] filter means Participants and anonymous
/// visitors can never reach these actions, even by typing the URL directly.
/// </summary>
[Area("Organiser")]
[RoleAuthorize(Roles.Organiser)]
public abstract class OrganiserControllerBase : RaceDayControllerBase
{
    protected OrganiserControllerBase(IRaceDayApiClient api) : base(api)
    {
    }

    /// <summary>
    /// Loads an event and confirms it belongs to the logged-in Organiser. Returns a ready-made
    /// error result (not found / access denied) when it doesn't, so actions can simply return it.
    /// The API performs the same ownership check on every write.
    /// </summary>
    protected async Task<(EventDto? Event, IActionResult? Failure)> LoadOwnedEventAsync(int eventId)
    {
        var result = await Api.GetEventAsync(eventId);
        if (!result.Success || result.Data == null)
        {
            return (null, ApiFailure(result, Url.Action("Index", "Events", new { area = "Organiser" })));
        }

        if (result.Data.OrganiserUserId != CurrentUser.UserId)
        {
            return (null, RedirectToAction("AccessDenied", "Account", new { area = "" }));
        }

        return (result.Data, null);
    }
}
