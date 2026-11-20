using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>Capture, correct and publish finish times and positions once an event has taken place.</summary>
public class ResultsController : OrganiserControllerBase
{
    public ResultsController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (eventItem, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
        {
            return ApiFailure(enrolments);
        }

        return View(new ResultsPageViewModel
        {
            Event = eventItem!,
            ActiveTab = "Results",
            Rows = BuildRows(enrolments.Data ?? new())
        });
    }

    /// <summary>Creates the result when none exists yet, otherwise updates it.</summary>
    [HttpPost]
    public async Task<IActionResult> Save(int eventId, ResultRowViewModel row)
    {
        var (_, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        if (string.IsNullOrWhiteSpace(row.FinishTime) || !row.FinishingPosition.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Enter both a finish time and a position.");
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            Error($"{row.ParticipantName}: {message}");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var finishTime = NormaliseTime(row.FinishTime!);
        var result = row.ResultId.HasValue
            ? await Api.UpdateResultAsync(row.ResultId.Value, new ResultUpdateRequest
            {
                FinishTime = finishTime,
                FinishingPosition = row.FinishingPosition!.Value,
                IsPublished = row.IsPublished
            })
            : await Api.CreateResultAsync(new ResultCreateRequest
            {
                EnrolmentId = row.EnrolmentId,
                FinishTime = finishTime,
                FinishingPosition = row.FinishingPosition!.Value,
                IsPublished = row.IsPublished
            });

        if (result.Success)
        {
            Success($"Result saved for {row.ParticipantName}{(row.IsPublished ? " and published" : " as a draft")}.");
        }
        else
        {
            Error($"{row.ParticipantName}: {result.ErrorMessage}");
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Publishes every captured draft result so Participants can see them.</summary>
    [HttpPost]
    public async Task<IActionResult> PublishAll(int eventId)
    {
        var (_, failure) = await LoadOwnedEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        var drafts = (enrolments.Data ?? new()).Where(e => e.ResultId.HasValue && !e.ResultPublished).ToList();
        var failed = 0;

        foreach (var draft in drafts)
        {
            var result = await Api.UpdateResultAsync(draft.ResultId!.Value, new ResultUpdateRequest
            {
                FinishTime = draft.FinishTime!.Value.ToString(@"hh\:mm\:ss"),
                FinishingPosition = draft.FinishingPosition!.Value,
                IsPublished = true
            });
            if (!result.Success) failed++;
        }

        if (drafts.Count == 0)
        {
            Info("There are no draft results to publish.");
        }
        else if (failed == 0)
        {
            Success($"{drafts.Count} {(drafts.Count == 1 ? "result" : "results")} published.");
        }
        else
        {
            Error($"{drafts.Count - failed} published, {failed} failed. Please try again.");
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Rows sorted with captured results first (by position), then entrants still to be timed.</summary>
    private static List<ResultRowViewModel> BuildRows(IEnumerable<EventEnrolmentDto> enrolments) => enrolments
        .OrderBy(e => e.FinishingPosition ?? int.MaxValue)
        .ThenBy(e => e.Participant.LastName)
        .Select(e => new ResultRowViewModel
        {
            EnrolmentId = e.EnrolmentId,
            ResultId = e.ResultId,
            ParticipantName = e.Participant.FullName,
            Category = e.Category,
            EnrolmentStatus = e.Status,
            FinishTime = e.FinishTime?.ToString(@"hh\:mm\:ss"),
            FinishingPosition = e.FinishingPosition,
            IsPublished = e.ResultPublished
        })
        .ToList();

    /// <summary>Accepts "1:24:35" as well as "01:24:35" and sends the API's hh:mm:ss format.</summary>
    private static string NormaliseTime(string value)
    {
        var parts = value.Trim().Split(':');
        return $"{int.Parse(parts[0]):00}:{parts[1]}:{parts[2]}";
    }
}
