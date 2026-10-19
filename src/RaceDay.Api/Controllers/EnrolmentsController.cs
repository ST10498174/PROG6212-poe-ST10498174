using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant in an event using a category from that event.</summary>
    [HttpPost("enrolments")]
    public async Task<IActionResult> CreateEnrolment(EnrolmentRequest request)
    {
        var accessError = RequireRole("Participant");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(request.EventId);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (category == null) return BadRequest(new { message = "The selected category does not belong to this event." });

        var alreadyEnrolled = await _db.Enrolments.AnyAsync(e => e.ParticipantUserId == CurrentUserId && e.EventId == request.EventId);
        if (alreadyEnrolled) return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantUserId = CurrentUserId!.Value,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            Status = "Pending",
            EnrolmentDate = DateTime.UtcNow
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return Created("/api/enrolments/me", enrolment);
    }

    /// <summary>Returns all enrolments belonging to the logged-in Participant.</summary>
    [HttpGet("enrolments/me")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var accessError = RequireRole("Participant");
        if (accessError != null) return accessError;

        var enrolments = await _db.Enrolments
            .Where(e => e.ParticipantUserId == CurrentUserId)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event!.Name,
                e.CategoryId,
                CategoryName = e.Category!.Name,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Returns all enrolments for an event owned by the logged-in Organiser.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(eventId);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only view enrolments for your own events." });

        var enrolments = await _db.Enrolments
            .Where(e => e.EventId == eventId)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .OrderBy(e => e.EnrolmentId)
            .Select(e => new
            {
                e.EnrolmentId,
                Participant = new
                {
                    e.Participant!.UserId,
                    e.Participant.FirstName,
                    e.Participant.LastName,
                    e.Participant.Email
                },
                Category = e.Category!.Name,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}
