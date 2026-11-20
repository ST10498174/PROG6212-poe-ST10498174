using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

/// <summary>Entering events, viewing "My events" and withdrawing from an upcoming event.</summary>
public class EnrolmentsController : ParticipantControllerBase
{
    public EnrolmentsController(IRaceDayApiClient api) : base(api)
    {
    }

    /// <summary>All of the Participant's entries with colour-coded statuses.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var enrolments = await Api.GetMyEnrolmentsAsync();
        if (!enrolments.Success)
        {
            return ApiFailure(enrolments);
        }

        return View(new MyEventsViewModel { Enrolments = enrolments.Data ?? new() });
    }

    /// <summary>Enters the event in the chosen category. The enrolment is recorded by the API immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int eventId, int? selectedCategoryId)
    {
        if (!selectedCategoryId.HasValue)
        {
            Error("Choose a category before entering.");
            return RedirectToAction("Details", "Events", new { area = "", id = eventId });
        }

        var result = await Api.EnrolAsync(eventId, selectedCategoryId.Value);
        if (!result.Success)
        {
            // e.g. 409 "You are already enrolled in this event." or 400 for a category from another event.
            Error(result.ErrorMessage ?? "You couldn't be entered into this event.");
            return RedirectToAction("Details", "Events", new { area = "", id = eventId });
        }

        Success("You're entered. Your entry is pending until the organiser confirms it.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id, string? eventName = null)
    {
        var result = await Api.WithdrawAsync(id);
        if (result.Success)
        {
            Success(string.IsNullOrWhiteSpace(eventName) ? "You've withdrawn from the event." : $"You've withdrawn from {eventName}.");
        }
        else
        {
            Error(result.ErrorMessage ?? "Your entry couldn't be withdrawn.");
        }

        return RedirectToAction(nameof(Index));
    }
}
