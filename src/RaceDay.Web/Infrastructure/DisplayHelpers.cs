using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>Small formatting helpers shared by the Razor views so every page presents data the same way.</summary>
public static class DisplayHelpers
{
    /// <summary>
    /// The event's banner from Blob Storage, or a bundled illustration for its event type when
    /// the Organiser hasn't uploaded one yet.
    /// </summary>
    public static string BannerUrl(string? bannerImageUrl, string eventType) =>
        !string.IsNullOrWhiteSpace(bannerImageUrl)
            ? bannerImageUrl
            : $"/images/banner-{(eventType ?? "run").ToLowerInvariant() switch { "walk" => "walk", "cycle" => "cycle", _ => "run" }}.svg";

    /// <summary>1 → "1st", 22 → "22nd", 47 → "47th", 113 → "113th".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        var suffix = lastTwo is >= 11 and <= 13 ? "th" : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return number.ToString(CultureInfo.InvariantCulture) + suffix;
    }

    /// <summary>Finish time as h:mm:ss (or mm:ss under an hour).</summary>
    public static string FinishTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{time.Minutes}:{time.Seconds:00}";

    /// <summary>Average pace per kilometre for runs and walks, or average speed for rides.</summary>
    public static string Pace(TimeSpan time, decimal distanceKm, string eventType)
    {
        if (distanceKm <= 0 || time <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        if (eventType == "Cycle")
        {
            var kmh = (double)distanceKm / time.TotalHours;
            return $"{kmh:0.0} km/h";
        }

        var perKm = TimeSpan.FromSeconds(time.TotalSeconds / (double)distanceKm);
        return $"{(int)perKm.TotalMinutes}:{perKm.Seconds:00} /km";
    }

    public static string Distance(decimal km) => $"{km:0.##} km";

    /// <summary>Friendly relative date, e.g. "Today", "Tomorrow", "In 12 days", "3 days ago".</summary>
    public static string Relative(DateTime date)
    {
        var days = (date.Date - DateTime.Today).Days;
        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            > 1 => $"In {days} days",
            _ => $"{-days} days ago"
        };
    }

    /// <summary>CSS class for an enrolment status badge (green Confirmed, amber Pending).</summary>
    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status status-confirmed",
        "Pending" => "status status-pending",
        _ => "status status-draft"
    };
}
