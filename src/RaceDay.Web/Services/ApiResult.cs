using System.Net;

namespace RaceDay.Web.Services;

/// <summary>
/// Outcome of a call to the RaceDay API. Controllers check <see cref="Success"/> and either use
/// <see cref="Data"/> or show <see cref="ErrorMessage"/> to the user, so raw exceptions and
/// HTTP details never reach the browser.
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>A friendly message suitable for display (taken from the API's "message" field where possible).</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Per-field validation errors returned by the API (property name to messages).</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;

    public static ApiResult Ok(HttpStatusCode status) => new() { Success = true, StatusCode = status };

    public static ApiResult Fail(HttpStatusCode status, string message, IReadOnlyDictionary<string, string[]>? fieldErrors = null) => new()
    {
        Success = false,
        StatusCode = status,
        ErrorMessage = message,
        FieldErrors = fieldErrors ?? new Dictionary<string, string[]>()
    };
}

/// <summary>An <see cref="ApiResult"/> that also carries deserialised response data.</summary>
public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(T? data, HttpStatusCode status) => new() { Success = true, StatusCode = status, Data = data };

    public static ApiResult<T> From(ApiResult failure) => new()
    {
        Success = false,
        StatusCode = failure.StatusCode,
        ErrorMessage = failure.ErrorMessage,
        FieldErrors = failure.FieldErrors
    };
}

/// <summary>
/// Thrown when the API rejects a logged-in user's request with 401, meaning the API session has expired.
/// <see cref="Infrastructure.ApiSessionExpiredFilter"/> catches it, clears the MVC session and sends the user to the login page.
/// </summary>
public class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException() : base("The RaceDay API session has expired.") { }
}
