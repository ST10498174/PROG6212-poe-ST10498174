using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string CategoryType { get; set; } = string.Empty;

    public short? MinAge { get; set; }
    public short? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}
