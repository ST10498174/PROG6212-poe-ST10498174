using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>Create, edit and delete the Organiser's own events, including the banner image upload.</summary>
public class EventsController : OrganiserControllerBase
{
    // API field names mapped onto this form's property names for error display.
    private static readonly Dictionary<string, string> FieldMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Date"] = nameof(EventFormViewModel.Date),
        ["DistanceKm"] = nameof(EventFormViewModel.DistanceKm)
    };

    public EventsController(IRaceDayApiClient api) : base(api)
    {
    }

    /// <summary>All of the Organiser's events with entry counts and quick links.</summary>
    public async Task<IActionResult> Index()
    {
        var events = await Api.GetMyEventsAsync();
        if (!events.Success)
        {
            return ApiFailure(events);
        }

        return View(new OrganiserDashboardViewModel { Events = events.Data ?? new() });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormViewModel { Date = DateTime.Today.AddDays(30) });

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateForm(model, isNew: true);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var created = await Api.CreateEventAsync(model.ToApiRequest());
        if (!created.Success || created.Data == null)
        {
            AddApiErrors(created, FieldMap);
            return View("Form", model);
        }

        var eventId = created.Data.EventId;

        // The banner is uploaded after the event exists because the API stores it against the event ID.
        if (model.Banner != null)
        {
            var upload = await Api.UploadEventBannerAsync(eventId, model.Banner);
            if (!upload.Success)
            {
                Error($"The event was created, but the banner couldn't be uploaded: {upload.ErrorMessage} You can add it from the edit page.");
                return RedirectToAction("Index", "Categories", new { eventId });
            }
        }

        Success($"\"{model.Name}\" is created. Next, add the categories participants can enter.");
        return RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(id);
        if (failure != null) return failure;

        ViewData["ManagedEvent"] = new ManagedEventViewModel { Event = eventItem!, ActiveTab = "Edit" };
        return View("Form", new EventFormViewModel
        {
            EventId = eventItem!.EventId,
            Name = eventItem.Name,
            Description = eventItem.Description,
            Date = eventItem.Date,
            Location = eventItem.Location,
            DistanceKm = eventItem.DistanceKm,
            EventType = eventItem.EventType,
            CurrentBannerUrl = eventItem.BannerImageUrl
        });
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(id);
        if (failure != null) return failure;

        model.EventId = id;
        model.CurrentBannerUrl = eventItem!.BannerImageUrl;
        ViewData["ManagedEvent"] = new ManagedEventViewModel { Event = eventItem, ActiveTab = "Edit" };

        ValidateForm(model, isNew: false);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var updated = await Api.UpdateEventAsync(id, model.ToApiRequest());
        if (!updated.Success)
        {
            AddApiErrors(updated, FieldMap);
            return View("Form", model);
        }

        if (model.Banner != null)
        {
            var upload = await Api.UploadEventBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                Error($"Event details were saved, but the banner couldn't be uploaded: {upload.ErrorMessage}");
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        Success("Event saved.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>Confirmation page that explains what deleting removes.</summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var events = await Api.GetMyEventsAsync();
        var eventItem = events.Data?.FirstOrDefault(e => e.EventId == id);
        if (eventItem == null)
        {
            var (_, failure) = await LoadOwnedEventAsync(id);
            return failure ?? RedirectToAction(nameof(Index));
        }

        return View(eventItem);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(id);
        if (failure != null) return failure;

        var result = await Api.DeleteEventAsync(id);
        if (!result.Success)
        {
            // e.g. 409 Conflict when participants have already entered.
            Error(result.ErrorMessage ?? "The event couldn't be deleted.");
            return RedirectToAction(nameof(Delete), new { id });
        }

        Success($"\"{eventItem!.Name}\" was deleted.");
        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(EventFormViewModel model, bool isNew)
    {
        if (isNew && model.Date.HasValue && model.Date.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.Date), "New events must be scheduled for today or later.");
        }

        if (model.Banner != null)
        {
            ImageUploadRules.Validate(model.Banner, ModelState, nameof(model.Banner));
        }
    }
}
