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
