using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

/// <summary>Search and filter values for the event listings (bound from the query string).</summary>
public class EventFilterViewModel
{
    [StringLength(100)]
    public string? Search { get; set; }

    [Display(Name = "Type")]
    public string? EventType { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType)
                              || !string.IsNullOrWhiteSpace(Location) || From.HasValue || To.HasValue;

    public EventFilter ToApiFilter() => new()
    {
        Search = Search,
        EventType = EventType,
        Location = Location,
        From = From,
        To = To,
        UpcomingOnly = true
    };
}

/// <summary>A filterable list of upcoming events (public events page).</summary>
public class EventListViewModel
{
    public EventFilterViewModel Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
    public string? LoadError { get; set; }
}

/// <summary>The public event page, including what the current visitor is allowed to do.</summary>
public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    /// <summary>Set when the logged-in Participant has already entered this event.</summary>
    public MyEnrolmentDto? MyEnrolment { get; set; }

    public bool IsParticipant { get; set; }
    public bool IsOrganiser { get; set; }
    public bool IsOwner { get; set; }

    [Display(Name = "Category")]
    public int? SelectedCategoryId { get; set; }
}

/// <summary>Create/edit form for an event, including the optional banner upload.</summary>
public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name."), StringLength(120)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe the event for participants."), StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter where the event starts."), StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.1, 9999.99, ErrorMessage = "Distance must be between 0.1 and 9999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose run, walk or cycle.")]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Choose run, walk or cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    /// <summary>The banner currently stored in Blob Storage (edit form only).</summary>
    public string? CurrentBannerUrl { get; set; }

    public bool IsEdit => EventId.HasValue;

    public EventRequest ToApiRequest() => new()
    {
        Name = Name,
        Description = Description,
        Date = Date!.Value.Date,
        Location = Location,
        DistanceKm = DistanceKm!.Value,
        EventType = EventType
    };
}
