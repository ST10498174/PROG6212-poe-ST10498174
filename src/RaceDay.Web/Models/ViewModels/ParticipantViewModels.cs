using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

/// <summary>Participant home: their next race, a few personal stats and the filterable list of upcoming events.</summary>
public class ParticipantDashboardViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public EventListViewModel Browse { get; set; } = new();
    public List<MyEnrolmentDto> Enrolments { get; set; } = new();
    public int ResultCount { get; set; }

    public MyEnrolmentDto? NextRace => Enrolments.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).FirstOrDefault();
    public int UpcomingCount => Enrolments.Count(e => e.IsUpcoming);

    /// <summary>Event IDs the participant has already entered, used to label cards "Entered".</summary>
    public HashSet<int> EnteredEventIds => Enrolments.Select(e => e.EventId).ToHashSet();
}

/// <summary>"My events": every enrolment split into upcoming and past.</summary>
public class MyEventsViewModel
{
    public List<MyEnrolmentDto> Enrolments { get; set; } = new();

    public IEnumerable<MyEnrolmentDto> Upcoming => Enrolments.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate);
    public IEnumerable<MyEnrolmentDto> Past => Enrolments.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate);
}

/// <summary>"My results": published results plus completed events still waiting for results.</summary>
public class MyResultsViewModel
{
    public List<MyResultDto> Results { get; set; } = new();
    public List<MyEnrolmentDto> AwaitingResults { get; set; } = new();

    public int RacesFinished => Results.Count;
    public decimal TotalDistance => Results.Sum(r => r.DistanceKm);
    public MyResultDto? BestFinish => Results.OrderBy(r => r.FinishingPosition).ThenBy(r => r.Date).FirstOrDefault();
}
