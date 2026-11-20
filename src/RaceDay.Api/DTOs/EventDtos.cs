using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty;
}

/// <summary>
/// Optional query-string filters for GET /api/events (Part 3 participant search).
/// Every filter is optional, so calling the endpoint with no parameters still returns all events.
/// </summary>
public class EventQuery
{
    /// <summary>Matches the event name, description or location.</summary>
    [MaxLength(100)]
    public string? Search { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    public string? EventType { get; set; }

    /// <summary>Matches part of the event location, for example "Durban".</summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>Only events on or after this date.</summary>
    public DateTime? From { get; set; }

    /// <summary>Only events on or before this date.</summary>
    public DateTime? To { get; set; }

    /// <summary>When true, only events from today onwards are returned.</summary>
    public bool UpcomingOnly { get; set; }
}
