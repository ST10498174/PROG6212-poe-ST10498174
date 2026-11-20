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

        // A flat object is returned rather than the EF entity: the tracked entity graph has
        // Event <-> Enrolment back-references that the JSON serializer cannot write (object cycle).
        return Created("/api/enrolments/me", new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantUserId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
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
                e.Status,
                // Part 3: extra event details so the "My Events" page can be built from a single API call.
                EventDate = e.Event!.EventDate,
                e.Event!.Location,
                e.Event!.EventType,
                e.Event!.DistanceKm,
                e.Event!.BannerImageUrl,
                HasResult = e.Result != null,
                ResultPublished = e.Result != null && e.Result.IsPublished
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
                    e.Participant.Email,
                    e.Participant.PhoneNumber
                },
                Category = e.Category!.Name,
                e.CategoryId,
                e.EnrolmentDate,
                e.Status,
                // Part 3: the captured result (if any) so the Organiser's results page can show and edit it.
                ResultId = e.Result != null ? (int?)e.Result.ResultId : null,
                FinishTime = e.Result != null ? (TimeSpan?)e.Result.FinishTime : null,
                FinishingPosition = e.Result != null ? (int?)e.Result.FinishingPosition : null,
                ResultPublished = e.Result != null && e.Result.IsPublished
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Confirms an enrolment (or sets it back to Pending) for an event owned by the logged-in Organiser.</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Status is not Pending or Confirmed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("enrolments/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnrolmentStatus(int id, EnrolmentStatusRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var status = NormalizeStatus(request.Status);
        if (status != "Pending" && status != "Confirmed")
        {
            return BadRequest(new { message = "Status must be Pending or Confirmed." });
        }

        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return NotFound(new { message = "Enrolment was not found." });
        if (enrolment.Event!.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only manage enrolments for your own events." });

        enrolment.Status = status;
        await _db.SaveChangesAsync();

        return Ok(new { enrolment.EnrolmentId, enrolment.EventId, enrolment.Status });
    }

    /// <summary>Withdraws the logged-in Participant from an upcoming event they entered.</summary>
    /// <response code="204">Enrolment removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">The event has already taken place or a result has been captured.</response>
    [HttpDelete("enrolments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> WithdrawEnrolment(int id)
    {
        var accessError = RequireRole("Participant");
        if (accessError != null) return accessError;

        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return NotFound(new { message = "Enrolment was not found." });
        if (enrolment.ParticipantUserId != CurrentUserId) return StatusCode(403, new { message = "You may only withdraw your own enrolments." });

        if (enrolment.Event!.EventDate.Date < DateTime.Today)
        {
            return Conflict(new { message = "You cannot withdraw from an event that has already taken place." });
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
        {
            return Conflict(new { message = "You cannot withdraw because a result has already been captured." });
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeStatus(string status)
    {
        var value = status.Trim();
        if (value.Length == 0) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
