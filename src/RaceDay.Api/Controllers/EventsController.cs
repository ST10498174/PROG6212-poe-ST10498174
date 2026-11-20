using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService? _blobStorage;
    private static readonly string[] ValidEventTypes = ["Run", "Walk", "Cycle"];

    // The blob service is optional so the Part 2 unit tests can still create the controller with only a DbContext.
    public EventsController(RaceDayDbContext db, IBlobStorageService? blobStorage = null)
    {
        _db = db;
        _blobStorage = blobStorage;
    }

    /// <summary>Returns RaceDay events, optionally filtered. This endpoint is public.</summary>
    /// <remarks>
    /// All query parameters are optional: search (name, description or location), eventType (Run, Walk, Cycle),
    /// location, from, to and upcomingOnly. An empty array is returned when no events match.
    /// </remarks>
    /// <response code="200">A (possibly empty) list of events ordered by date.</response>
    /// <response code="400">An invalid event type or date range was supplied.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents([FromQuery] EventQuery? query = null)
    {
        query ??= new EventQuery();

        if (!string.IsNullOrWhiteSpace(query.EventType) && !ValidEventTypes.Contains(NormalizeEventType(query.EventType)))
        {
            return BadRequest(new { message = "Event type must be Run, Walk, or Cycle." });
        }

        if (query.From.HasValue && query.To.HasValue && query.From.Value.Date > query.To.Value.Date)
        {
            return BadRequest(new { message = "The 'from' date must be on or before the 'to' date." });
        }

        var events = _db.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            events = events.Where(e => e.Name.Contains(term) || e.Description.Contains(term) || e.Location.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            var type = NormalizeEventType(query.EventType);
            events = events.Where(e => e.EventType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim();
            events = events.Where(e => e.Location.Contains(location));
        }

        if (query.UpcomingOnly)
        {
            var today = DateTime.Today;
            events = events.Where(e => e.EventDate >= today);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value.Date;
            events = events.Where(e => e.EventDate >= from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.Date;
            events = events.Where(e => e.EventDate <= to);
        }

        var result = await events
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.Name)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                Date = e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                e.OrganiserUserId,
                OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
                CategoryCount = e.Categories.Count
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>Returns the logged-in Organiser's own events with enrolment and result statistics for the dashboard.</summary>
    /// <response code="200">The Organiser's events (empty array when none have been created yet).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEvents()
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var organiserId = CurrentUserId!.Value;
        var events = await _db.Events
            .Where(e => e.OrganiserUserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                Date = e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                CategoryCount = e.Categories.Count,
                EnrolmentCount = e.Enrolments.Count,
                ConfirmedCount = e.Enrolments.Count(en => en.Status == "Confirmed"),
                PendingCount = e.Enrolments.Count(en => en.Status == "Pending"),
                ResultCount = e.Enrolments.Count(en => en.Result != null),
                PublishedResultCount = e.Enrolments.Count(en => en.Result != null && en.Result.IsPublished)
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns the full details of one event. This endpoint is public.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventItem = await _db.Events
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                Date = e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                e.OrganiserUserId,
                OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName
            })
            .FirstOrDefaultAsync();

        return eventItem == null
            ? NotFound(new { message = "Event was not found." })
            : Ok(eventItem);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var validationError = ValidateEventRequest(request);
        if (validationError != null) return validationError;

        var eventItem = new Event
        {
            OrganiserUserId = CurrentUserId!.Value,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.Date.Date,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = NormalizeEventType(request.EventType),
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(eventItem);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = eventItem.EventId }, ToResponse(eventItem));
    }

    /// <summary>Updates an event belonging to the logged-in Organiser.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var validationError = ValidateEventRequest(request);
        if (validationError != null) return validationError;

        var eventItem = await _db.Events.FindAsync(id);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only update your own events." });

        eventItem.Name = request.Name.Trim();
        eventItem.Description = request.Description.Trim();
        eventItem.EventDate = request.Date.Date;
        eventItem.Location = request.Location.Trim();
        eventItem.DistanceKm = request.DistanceKm;
        eventItem.EventType = NormalizeEventType(request.EventType);

        await _db.SaveChangesAsync();
        return Ok(ToResponse(eventItem));
    }

    /// <summary>Deletes an Organiser's event when no enrolments depend on it.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(id);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only delete your own events." });

        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            return Conflict(new { message = "This event cannot be deleted because enrolments already exist." });
        }

        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();
        var bannerUrl = eventItem.BannerImageUrl;
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(eventItem);
        await _db.SaveChangesAsync();

        // Part 3: remove the event's banner from Blob Storage once the event itself is gone.
        if (_blobStorage != null)
        {
            await _blobStorage.DeleteImageAsync(bannerUrl);
        }

        return NoContent();
    }

    // Same shape as GET /api/events/{id}; avoids serialising EF navigation properties.
    private static object ToResponse(Event e) => new
    {
        e.EventId,
        e.Name,
        e.Description,
        Date = e.EventDate,
        e.Location,
        e.DistanceKm,
        e.EventType,
        e.BannerImageUrl,
        e.OrganiserUserId
    };

    private IActionResult? ValidateEventRequest(EventRequest request)
    {
        if (request.DistanceKm <= 0)
        {
            return BadRequest(new { message = "Distance must be greater than zero." });
        }

        if (!ValidEventTypes.Contains(NormalizeEventType(request.EventType)))
        {
            return BadRequest(new { message = "Event type must be Run, Walk, or Cycle." });
        }

        return null;
    }

    private static string NormalizeEventType(string eventType)
    {
        var value = eventType.Trim();
        if (value.Length == 0) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
