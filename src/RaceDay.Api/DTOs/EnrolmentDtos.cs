using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>Body for PUT /api/enrolments/{id}/status, used by Organisers to confirm or reset an enrolment.</summary>
public class EnrolmentStatusRequest
{
    /// <summary>Pending or Confirmed (the two statuses allowed by the database check constraint).</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
