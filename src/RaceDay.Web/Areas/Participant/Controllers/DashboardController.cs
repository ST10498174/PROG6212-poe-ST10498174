using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Controllers;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

/// <summary>Participant home page: next race, personal stats and all upcoming events with filters.</summary>
public class DashboardController : ParticipantControllerBase
{
    public DashboardController(IRaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        // Calls run one after another because they share the user's session (which is not thread-safe).
        var browse = await EventsController.LoadEventListAsync(Api, filter, ModelState);
        var enrolments = await Api.GetMyEnrolmentsAsync();
        var results = await Api.GetMyResultsAsync();

        return View(new ParticipantDashboardViewModel
        {
            FirstName = CurrentUser.FirstName,
            Browse = browse,
            Enrolments = enrolments.Data ?? new(),
            ResultCount = results.Data?.Count ?? 0
        });
    }
}
