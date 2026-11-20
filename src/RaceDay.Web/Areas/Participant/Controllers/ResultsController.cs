using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

/// <summary>The Participant's personal race history: finish times and positions across completed events.</summary>
public class ResultsController : ParticipantControllerBase
{
    public ResultsController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var resultsCall = await Api.GetMyResultsAsync();
        if (!resultsCall.Success)
        {
            return ApiFailure(resultsCall);
        }

        var enrolments = await Api.GetMyEnrolmentsAsync();
        var results = resultsCall.Data ?? new();
        var resultEventIds = results.Select(r => r.EventId).ToHashSet();

        return View(new MyResultsViewModel
        {
            Results = results.OrderByDescending(r => r.Date).ToList(),

            // Completed events the Participant entered that have no published result yet.
            AwaitingResults = (enrolments.Data ?? new())
                .Where(e => !e.IsUpcoming && !resultEventIds.Contains(e.EventId))
                .OrderByDescending(e => e.EventDate)
                .ToList()
        });
    }
}
