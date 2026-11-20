namespace RaceDay.Web.Infrastructure;

/// <summary>Names of the values stored in the MVC server-side session after a successful login.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string Email = "Email";
    public const string ProfilePictureUrl = "ProfilePictureUrl";

    /// <summary>
    /// The API's own session cookie (".RaceDay.Session=..."). The MVC app stores it server-side and
    /// replays it on every API call, so the API knows which user is making the request.
    /// It is never sent to the browser.
    /// </summary>
    public const string ApiSessionCookie = "ApiSessionCookie";
}

/// <summary>The two RaceDay roles, matching the values stored by the API.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
