using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Captures a result for an enrolment in an event owned by the logged-in Organiser.</summary>
    [HttpPost("results")]
    public async Task<IActionResult> CreateResult(ResultCreateRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        if (!TryParseFinishTime(request.FinishTime, out var finishTime))
        {
            return BadRequest(new { message = "FinishTime must be in HH:mm:ss format, for example 01:24:35." });
        }

        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null) return NotFound(new { message = "Enrolment was not found." });
        if (enrolment.Event!.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only capture results for your own events." });
        if (enrolment.Event.EventDate.Date > DateTime.Today) return BadRequest(new { message = "A result can only be captured after the event has taken place." });
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId)) return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = finishTime,
            FinishingPosition = request.FinishingPosition,
            IsPublished = request.IsPublished,
            RecordedAt = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return Created($"/api/results/{result.ResultId}", result);
    }

    /// <summary>Updates a result captured for an event owned by the logged-in Organiser.</summary>
    [HttpPut("results/{id:int}")]
    public async Task<IActionResult> UpdateResult(int id, ResultUpdateRequest request)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        if (!TryParseFinishTime(request.FinishTime, out var finishTime))
        {
            return BadRequest(new { message = "FinishTime must be in HH:mm:ss format, for example 01:24:35." });
        }

        var result = await _db.Results.Include(r => r.Enrolment)!.ThenInclude(e => e!.Event).FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound(new { message = "Result was not found." });
        if (result.Enrolment!.Event!.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only update results for your own events." });

        result.FinishTime = finishTime;
        result.FinishingPosition = request.FinishingPosition;
        result.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync();
        return Ok(result);
    }

    /// <summary>Returns the logged-in Participant's own published results.</summary>
    [HttpGet("results/me")]
    public async Task<IActionResult> GetMyResults()
    {
        var accessError = RequireRole("Participant");
        if (accessError != null) return accessError;

        var results = await _db.Results
            .Where(r => r.IsPublished && r.Enrolment!.ParticipantUserId == CurrentUserId)
            .Include(r => r.Enrolment)!.ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment)!.ThenInclude(e => e!.Category)
            .OrderByDescending(r => r.Enrolment!.Event!.EventDate)
            .Select(r => new
            {
                r.ResultId,
                EventName = r.Enrolment!.Event!.Name,
                Date = r.Enrolment.Event.EventDate,
                Category = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishingPosition
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns all captured results for an event owned by the logged-in Organiser.</summary>
    [HttpGet("events/{eventId:int}/results")]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var accessError = RequireRole("Organiser");
        if (accessError != null) return accessError;

        var eventItem = await _db.Events.FindAsync(eventId);
        if (eventItem == null) return NotFound(new { message = "Event was not found." });
        if (eventItem.OrganiserUserId != CurrentUserId) return StatusCode(403, new { message = "You may only view results for your own events." });

        var results = await _db.Results
            .Where(r => r.Enrolment!.EventId == eventId)
            .Include(r => r.Enrolment)!.ThenInclude(e => e!.Participant)
            .OrderBy(r => r.FinishingPosition)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                Participant = new
                {
                    r.Enrolment!.Participant!.UserId,
                    r.Enrolment.Participant.FirstName,
                    r.Enrolment.Participant.LastName
                },
                r.FinishTime,
                r.FinishingPosition,
                r.IsPublished
            })
            .ToListAsync();

        return Ok(results);
    }

    private static bool TryParseFinishTime(string value, out TimeSpan finishTime)
    {
        return TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, out finishTime) && finishTime > TimeSpan.Zero;
    }
}
