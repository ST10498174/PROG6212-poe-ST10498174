using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

/// <summary>Organiser dashboard: headline numbers plus upcoming and recent events.</summary>
public class OrganiserDashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new();

    public IEnumerable<OrganiserEventDto> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.Date);
    public IEnumerable<OrganiserEventDto> Past => Events.Where(e => !e.IsUpcoming).OrderByDescending(e => e.Date);

    public OrganiserEventDto? NextEvent => Upcoming.FirstOrDefault();

    public int TotalEntries => Events.Sum(e => e.EnrolmentCount);
    public int PendingEntries => Events.Sum(e => e.PendingCount);

    /// <summary>Past events where entrants still have no published result.</summary>
    public IEnumerable<OrganiserEventDto> AwaitingResults => Past.Where(e => e.EnrolmentCount > 0 && e.PublishedResultCount < e.EnrolmentCount);
}

/// <summary>Shared header data for the event management pages (categories, entries, results).</summary>
public class ManagedEventViewModel
{
    public EventDto Event { get; set; } = new();

    /// <summary>The management tab being shown: Edit, Categories, Entries or Results.</summary>
    public string ActiveTab { get; set; } = string.Empty;
}

/// <summary>Add/edit form for an age or distance category.</summary>
public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Name the category, e.g. \"Senior\" or \"21.1km\"."), StringLength(100)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Age|Distance)$", ErrorMessage = "Choose age or distance.")]
    [Display(Name = "Category type")]
    public string CategoryType { get; set; } = "Distance";

    [Range(0, 120, ErrorMessage = "Enter an age from 0 to 120.")]
    [Display(Name = "Minimum age")]
    public short? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Enter an age from 0 to 120.")]
    [Display(Name = "Maximum age")]
    public short? MaxAge { get; set; }

    [Range(0.1, 9999.99, ErrorMessage = "Distance must be between 0.1 and 9999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    /// <summary>Sends only the fields relevant to the chosen type, as the API's rules require.</summary>
    public CategoryRequest ToApiRequest() => CategoryType == "Age"
        ? new CategoryRequest { Name = Name, CategoryType = "Age", MinAge = MinAge, MaxAge = MaxAge }
        : new CategoryRequest { Name = Name, CategoryType = "Distance", DistanceKm = DistanceKm };
}

public class CategoriesPageViewModel : ManagedEventViewModel
{
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsPageViewModel : ManagedEventViewModel
{
    public List<EventEnrolmentDto> Enrolments { get; set; } = new();

    /// <summary>All, Pending or Confirmed.</summary>
    public string StatusFilter { get; set; } = "All";

    public IEnumerable<EventEnrolmentDto> Filtered => StatusFilter switch
    {
        "Pending" => Enrolments.Where(e => e.Status == "Pending"),
        "Confirmed" => Enrolments.Where(e => e.Status == "Confirmed"),
        _ => Enrolments
    };
}

/// <summary>One editable row on the results entry page.</summary>
public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EnrolmentStatus { get; set; } = string.Empty;

    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss, e.g. 01:24:35.")]
    [Display(Name = "Finish time")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    [Display(Name = "Position")]
    public int? FinishingPosition { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; }
}

public class ResultsPageViewModel : ManagedEventViewModel
{
    public List<ResultRowViewModel> Rows { get; set; } = new();

    /// <summary>Results can only be captured once the event date has passed (API rule).</summary>
    public bool CanCapture => Event.Date.Date <= DateTime.Today;

    public int CapturedCount => Rows.Count(r => r.ResultId.HasValue);
    public int PublishedCount => Rows.Count(r => r.ResultId.HasValue && r.IsPublished);
}
