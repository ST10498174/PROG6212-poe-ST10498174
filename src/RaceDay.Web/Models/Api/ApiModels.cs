namespace RaceDay.Web.Models.Api;

// ------------------------------------------------------------------------------------------------
// These classes mirror the JSON returned and accepted by the Part 2 RaceDay Web API.
// They contain data only: no validation rules or business logic are duplicated here,
// because the API remains the single source of truth (Part 3 requirement 1).
// ------------------------------------------------------------------------------------------------

/// <summary>Response body of POST /api/auth/login.</summary>
public class LoginResponse
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>Request body of POST /api/auth/register.</summary>
public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>Response body of GET /api/profile.</summary>
public class ProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>Request body of PUT /api/profile.</summary>
public class ProfileUpdateRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}

/// <summary>An event as returned by GET /api/events and GET /api/events/{id}.</summary>
public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserUserId { get; set; }
    public string? OrganiserName { get; set; }
    public int CategoryCount { get; set; }

    /// <summary>True when the event is today or later (entries open).</summary>
    public bool IsUpcoming => Date.Date >= DateTime.Today;
}

/// <summary>An Organiser's event with statistics, from GET /api/events/mine.</summary>
public class OrganiserEventDto : EventDto
{
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int PendingCount { get; set; }
    public int ResultCount { get; set; }
    public int PublishedResultCount { get; set; }
}

/// <summary>Request body of POST/PUT /api/events.</summary>
public class EventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
}

/// <summary>Optional filters passed to GET /api/events as query-string parameters.</summary>
public class EventFilter
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool UpcomingOnly { get; set; } = true;
}

/// <summary>Category returned by GET /api/events/{eventId}/categories.</summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public short? MinAge { get; set; }
    public short? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }

    /// <summary>Human-readable description, for example "Ages 20-39" or "21.1 km".</summary>
    public string Summary => CategoryType == "Age"
        ? (MaxAge >= 100 ? $"Ages {MinAge}+" : $"Ages {MinAge}-{MaxAge}")
        : $"{DistanceKm:0.##} km";
}

/// <summary>Request body of POST /api/events/{eventId}/categories and PUT /api/categories/{id}.</summary>
public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public short? MinAge { get; set; }
    public short? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

/// <summary>A Participant's own enrolment from GET /api/enrolments/me.</summary>
public class MyEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string? BannerImageUrl { get; set; }
    public bool HasResult { get; set; }
    public bool ResultPublished { get; set; }

    public bool IsUpcoming => EventDate.Date >= DateTime.Today;
}

/// <summary>Participant summary nested inside an Organiser's enrolment list.</summary>
public class EnrolledParticipantDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>An enrolment for one of the Organiser's events, from GET /api/events/{eventId}/enrolments.</summary>
public class EventEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public EnrolledParticipantDto Participant { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ResultId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? FinishingPosition { get; set; }
    public bool ResultPublished { get; set; }
}

/// <summary>Request body of POST /api/enrolments.</summary>
public class EnrolmentRequest
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
}

/// <summary>Request body of POST /api/results.</summary>
public class ResultCreateRequest
{
    public int EnrolmentId { get; set; }
    public string FinishTime { get; set; } = string.Empty;
    public int FinishingPosition { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>Request body of PUT /api/results/{id}.</summary>
public class ResultUpdateRequest
{
    public string FinishTime { get; set; } = string.Empty;
    public int FinishingPosition { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>A published result from GET /api/results/me.</summary>
public class MyResultDto
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
}

/// <summary>Response body of the image upload endpoints.</summary>
public class ImageUploadResponse
{
    public string? BannerImageUrl { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>Minimal shape used when only the new record's identifier is needed.</summary>
public class CreatedEventResponse
{
    public int EventId { get; set; }
}
