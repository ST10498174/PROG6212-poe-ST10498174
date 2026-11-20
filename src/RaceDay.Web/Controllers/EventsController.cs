using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Public event browsing and the event detail page. These pages use the API's public endpoints,
/// so they work without logging in. What the detail page offers (enter, manage, log in) depends on
/// who is viewing it.
/// </summary>
public class EventsController : RaceDayControllerBase
{
    public EventsController(IRaceDayApiClient api) : base(api)
    {
    }

    /// <summary>All upcoming events with search and filters.</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        // Participants have the same listing on their dashboard, alongside their own stats.
        if (HttpContext.GetCurrentUser() is { IsParticipant: true })
        {
            return RedirectToAction("Index", "Dashboard", new
            {
                area = "Participant",
                filter.Search,
                filter.EventType,
                filter.Location,
                From = filter.From?.ToString("yyyy-MM-dd"),
                To = filter.To?.ToString("yyyy-MM-dd")
            });
        }

        var model = await LoadEventListAsync(Api, filter, ModelState);
        return View(model);
    }

    /// <summary>Full event information, categories and the enrolment panel.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetEventAsync(id);
        if (!eventResult.Success || eventResult.Data == null)
        {
            return ApiFailure(eventResult, Url.Action(nameof(Index)));
        }

        var categories = await Api.GetCategoriesAsync(id);
        var user = HttpContext.GetCurrentUser();

        var model = new EventDetailsViewModel
        {
            Event = eventResult.Data,
            Categories = categories.Success ? categories.Data ?? new() : new(),
            IsParticipant = user?.IsParticipant == true,
            IsOrganiser = user?.IsOrganiser == true,
            IsOwner = user?.IsOrganiser == true && user.UserId == eventResult.Data.OrganiserUserId
        };

        if (model.IsParticipant)
        {
            var mine = await Api.GetMyEnrolmentsAsync();
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id);
        }

        return View(model);
    }

    /// <summary>
    /// Shared by the public listing and the Participant dashboard: validates the filters,
    /// calls GET /api/events and returns a ready-to-render list (or a friendly error).
    /// </summary>
    internal static async Task<EventListViewModel> LoadEventListAsync(
        IRaceDayApiClient api, EventFilterViewModel filter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var model = new EventListViewModel { Filter = filter };

        if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
        {
            model.LoadError = "The 'from' date must be before the 'to' date.";
            return model;
        }

        if (!modelState.IsValid)
        {
            model.LoadError = "Some filter values weren't valid, so they were ignored.";
            filter = new EventFilterViewModel();
            model.Filter = filter;
        }

        var events = await api.GetEventsAsync(filter.ToApiFilter());
        if (events.Success)
        {
            model.Events = events.Data ?? new List<EventDto>();
        }
        else
        {
            model.LoadError = events.ErrorMessage;
        }

        return model;
    }
}
