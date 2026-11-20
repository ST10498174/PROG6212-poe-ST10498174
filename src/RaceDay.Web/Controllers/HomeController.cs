using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public landing page plus the friendly error and status-code pages.</summary>
public class HomeController : RaceDayControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRaceDayApiClient api, ILogger<HomeController> logger) : base(api)
    {
        _logger = logger;
    }

    /// <summary>Landing page for visitors. Logged-in users go straight to their role's dashboard.</summary>
    public async Task<IActionResult> Index()
    {
        var user = HttpContext.GetCurrentUser();
        if (user != null)
        {
            return Redirect(user.DashboardUrl());
        }

        // The public events endpoint needs no login; a failure simply hides the preview strip.
        var events = await Api.GetEventsAsync(new EventFilter { UpcomingOnly = true });
        return View(events.Success ? events.Data!.Take(3).ToList() : new List<EventDto>());
    }

    /// <summary>Shown for unhandled exceptions (outside Development) without exposing technical details.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature != null)
        {
            _logger.LogError(feature.Error, "Unhandled exception on {Path}.", feature.Path);
        }

        ViewData["Title"] = "Something went wrong";
        ViewData["Message"] = "We hit an unexpected problem. Your changes may not have been saved, so please try again.";
        return View("Problem");
    }

    /// <summary>Renders 404 and other status codes with the RaceDay page design.</summary>
    [Route("/Home/StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        ViewData["Title"] = code == 404 ? "Page not found" : "Something went wrong";
        ViewData["Message"] = code == 404
            ? "That page doesn't exist. Check the address, or head back to the events list."
            : "The request couldn't be completed. Please try again.";
        return View("Problem");
    }
}
