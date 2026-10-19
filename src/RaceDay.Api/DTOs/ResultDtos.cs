using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultCreateRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    [Required]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }

    public bool IsPublished { get; set; }
}

public class ResultUpdateRequest
{
    [Required]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }

    public bool IsPublished { get; set; }
}
