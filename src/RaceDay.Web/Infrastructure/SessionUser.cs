using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>The logged-in user as read from the MVC session.</summary>
public record SessionUser(int UserId, string Role, string FirstName, string LastName, string Email, string? ProfilePictureUrl)
{
    public bool IsOrganiser => Role == Roles.Organiser;
    public bool IsParticipant => Role == Roles.Participant;
    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpperInvariant();
}

/// <summary>Helpers for writing and reading the logged-in user to and from the session.</summary>
public static class SessionUserExtensions
{
    /// <summary>Returns the logged-in user, or null when nobody is logged in.</summary>
    public static SessionUser? GetCurrentUser(this HttpContext context)
    {
        var session = context.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);
        if (!userId.HasValue || string.IsNullOrEmpty(role))
        {
            return null;
        }

        return new SessionUser(
            userId.Value,
            role,
            session.GetString(SessionKeys.FirstName) ?? string.Empty,
            session.GetString(SessionKeys.LastName) ?? string.Empty,
            session.GetString(SessionKeys.Email) ?? string.Empty,
            session.GetString(SessionKeys.ProfilePictureUrl));
    }

    /// <summary>Stores the user's ID, role and display details in the session after login.</summary>
    public static void SignIn(this ISession session, LoginResponse user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FirstName, user.FirstName);
        session.SetString(SessionKeys.LastName, user.LastName);
        session.SetString(SessionKeys.Email, user.Email);
    }

    /// <summary>Keeps the cached name and picture shown in the navigation bar in step with profile changes.</summary>
    public static void UpdateDisplayDetails(this ISession session, ProfileDto profile)
    {
        session.SetString(SessionKeys.FirstName, profile.FirstName);
        session.SetString(SessionKeys.LastName, profile.LastName);
        session.SetString(SessionKeys.Email, profile.Email);
        if (string.IsNullOrEmpty(profile.ProfilePictureUrl))
        {
            session.Remove(SessionKeys.ProfilePictureUrl);
        }
        else
        {
            session.SetString(SessionKeys.ProfilePictureUrl, profile.ProfilePictureUrl);
        }
    }

    /// <summary>The landing page for each role after login.</summary>
    public static string DashboardUrl(this SessionUser user) =>
        user.IsOrganiser ? "/Organiser/Dashboard" : "/Participant/Dashboard";
}
