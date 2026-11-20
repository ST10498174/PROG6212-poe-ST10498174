using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>Everyone who has entered one of the Organiser's events, with confirm / set-to-pending actions.</summary>
public class EnrolmentsController : OrganiserControllerBase
{
    private static readonly string[] Statuses = ["Pending", "Confirmed"];

    public EnrolmentsController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId, string? status = null)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
        {
            return ApiFailure(enrolments);
        }

        return View(new EnrolmentsPageViewModel
        {
            Event = eventItem!,
            ActiveTab = "Entries",
            Enrolments = enrolments.Data ?? new(),
            StatusFilter = Statuses.Contains(status) ? status! : "All"
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int eventId, int enrolmentId, string status, string? filter = null)
    {
        if (!Statuses.Contains(status))
        {
            Error("Choose Pending or Confirmed.");
            return RedirectToAction(nameof(Index), new { eventId, status = filter });
        }

        var result = await Api.UpdateEnrolmentStatusAsync(enrolmentId, status);
        if (result.Success)
        {
            Success(status == "Confirmed" ? "Entry confirmed." : "Entry set back to pending.");
        }
        else
        {
            Error(result.ErrorMessage ?? "The entry couldn't be updated.");
        }

        return RedirectToAction(nameof(Index), new { eventId, status = filter });
    }

    /// <summary>Confirms every pending entry for the event in one action.</summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmAll(int eventId)
    {
        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
        {
            Error(enrolments.ErrorMessage ?? "Entries couldn't be loaded.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var pending = enrolments.Data!.Where(e => e.Status == "Pending").ToList();
        var failed = 0;
        foreach (var enrolment in pending)
        {
            var result = await Api.UpdateEnrolmentStatusAsync(enrolment.EnrolmentId, "Confirmed");
            if (!result.Success) failed++;
        }

        if (pending.Count == 0)
        {
            Info("There are no pending entries to confirm.");
        }
        else if (failed == 0)
        {
            Success($"{pending.Count} {(pending.Count == 1 ? "entry" : "entries")} confirmed.");
        }
        else
        {
            Error($"{pending.Count - failed} confirmed, {failed} couldn't be updated. Please try again.");
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
