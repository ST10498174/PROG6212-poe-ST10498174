using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

/// <summary>Organiser home: summary of their events, entries per event and upcoming dates.</summary>
public class DashboardController : OrganiserControllerBase
{
    public DashboardController(IRaceDayApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index()
    {
        var events = await Api.GetMyEventsAsync();
        if (!events.Success)
        {
            return ApiFailure(events);
        }

        return View(new OrganiserDashboardViewModel { Events = events.Data ?? new() });
    }
}
