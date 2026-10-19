using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private static readonly string[] ValidEventTypes = ["Run", "Walk", "Cycle"];

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns all available RaceDay events. This endpoint is public.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
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
                e.OrganiserUserId
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
                e.OrganiserUserId
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

        return CreatedAtAction(nameof(GetEvent), new { id = eventItem.EventId }, eventItem);
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
        return Ok(eventItem);
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
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(eventItem);
        await _db.SaveChangesAsync();

        return NoContent();
    }

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
