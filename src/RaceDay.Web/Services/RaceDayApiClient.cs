using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Settings for the "RaceDayApi" section of appsettings.json.</summary>
public class RaceDayApiOptions
{
    public const string SectionName = "RaceDayApi";

    /// <summary>Base address of the Part 2 Web API, e.g. http://localhost:5158/ locally or http://api:8080/ in Docker.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5158/";

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Typed HttpClient wrapper around every RaceDay API endpoint used by the MVC application.
///
/// Session relay: the API authenticates requests with its own session cookie. When a user logs in,
/// this client captures that cookie from the API's Set-Cookie header and stores it in the user's
/// MVC session (server-side). Every later request made on behalf of that user replays the cookie,
/// so the API's role checks apply to everything the MVC app does. The browser only ever holds the
/// MVC session cookie.
/// </summary>
public class RaceDayApiClient : IRaceDayApiClient
{
    private const string ApiSessionCookieName = ".RaceDay.Session";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    // ============================================================== Authentication

    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password)
    {
        // Any previous API session is discarded before logging in.
        Session?.Remove(SessionKeys.ApiSessionCookie);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { email, password }, options: JsonOptions)
        };

        return await SendAsync<LoginResponse>(request, captureSessionCookie: true);
    }

    public Task<ApiResult> RegisterAsync(RegisterRequest request) =>
        SendAsync(HttpMethod.Post, "api/auth/register", request);

    public async Task LogoutAsync()
    {
        // Ending the API session is best-effort: the MVC session is cleared by the caller regardless.
        try
        {
            if (!string.IsNullOrEmpty(Session?.GetString(SessionKeys.ApiSessionCookie)))
            {
                await SendAsync(HttpMethod.Post, "api/auth/logout", null, throwWhenSessionExpired: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API logout failed; continuing with local logout.");
        }
    }

    // ============================================================== Profile

    public Task<ApiResult<ProfileDto>> GetProfileAsync() => SendAsync<ProfileDto>(HttpMethod.Get, "api/profile");

    public Task<ApiResult> UpdateProfileAsync(ProfileUpdateRequest request) => SendAsync(HttpMethod.Put, "api/profile", request);

    public Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file) =>
        UploadFileAsync("api/profile/picture", file);

    // ============================================================== Events

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(EventFilter? filter = null) =>
        SendAsync<List<EventDto>>(HttpMethod.Get, "api/events" + BuildQueryString(filter));

    public Task<ApiResult<EventDto>> GetEventAsync(int eventId) => SendAsync<EventDto>(HttpMethod.Get, $"api/events/{eventId}");

    public Task<ApiResult<List<OrganiserEventDto>>> GetMyEventsAsync() => SendAsync<List<OrganiserEventDto>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<CreatedEventResponse>> CreateEventAsync(EventRequest request) =>
        SendAsync<CreatedEventResponse>(HttpMethod.Post, "api/events", request);

    public Task<ApiResult> UpdateEventAsync(int eventId, EventRequest request) => SendAsync(HttpMethod.Put, $"api/events/{eventId}", request);

    public Task<ApiResult> DeleteEventAsync(int eventId) => SendAsync(HttpMethod.Delete, $"api/events/{eventId}");

    public Task<ApiResult<ImageUploadResponse>> UploadEventBannerAsync(int eventId, IFormFile file) =>
        UploadFileAsync($"api/events/{eventId}/banner", file);

    // ============================================================== Categories

    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiResult> UpdateCategoryAsync(int categoryId, CategoryRequest request) =>
        SendAsync(HttpMethod.Put, $"api/categories/{categoryId}", request);

    public Task<ApiResult> DeleteCategoryAsync(int categoryId) => SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ============================================================== Enrolments

    public Task<ApiResult> EnrolAsync(int eventId, int categoryId) =>
        SendAsync(HttpMethod.Post, "api/enrolments", new EnrolmentRequest { EventId = eventId, CategoryId = categoryId });

    public Task<ApiResult<List<MyEnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<MyEnrolmentDto>>(HttpMethod.Get, "api/enrolments/me");

    public Task<ApiResult> WithdrawAsync(int enrolmentId) => SendAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    public Task<ApiResult<List<EventEnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EventEnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
        SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new { status });

    // ============================================================== Results

    public Task<ApiResult> CreateResultAsync(ResultCreateRequest request) => SendAsync(HttpMethod.Post, "api/results", request);

    public Task<ApiResult> UpdateResultAsync(int resultId, ResultUpdateRequest request) =>
        SendAsync(HttpMethod.Put, $"api/results/{resultId}", request);

    public Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync() => SendAsync<List<MyResultDto>>(HttpMethod.Get, "api/results/me");

    // ============================================================== Core HTTP plumbing

    private Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        return SendAndDisposeAsync<T>(request);
    }

    private async Task<ApiResult<T>> SendAndDisposeAsync<T>(HttpRequestMessage request)
    {
        using (request)
        {
            return await SendAsync<T>(request);
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null, bool throwWhenSessionExpired = true)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        // "object" is used as a placeholder type because these calls only need success or failure.
        return await SendAsync<object>(request, throwWhenSessionExpired: throwWhenSessionExpired);
    }

    private async Task<ApiResult<ImageUploadResponse>> UploadFileAsync(string url, IFormFile file)
    {
        // The MVC app never talks to Azure: the file is streamed to the API as multipart/form-data
        // and the API stores it in Blob Storage (Part 3 requirement 5).
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var type)
            ? type
            : new MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent { { fileContent, "file", Path.GetFileName(file.FileName) } };
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        return await SendAsync<ImageUploadResponse>(request);
    }

    /// <summary>
    /// Sends a request with the user's API session cookie attached and converts the response into an
    /// <see cref="ApiResult{T}"/>. Network failures become a friendly "service unavailable" result.
    /// </summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, bool captureSessionCookie = false, bool throwWhenSessionExpired = true)
    {
        var apiCookie = Session?.GetString(SessionKeys.ApiSessionCookie);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.Add("Cookie", apiCookie);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "RaceDay API could not be reached for {Method} {Url}.", request.Method, request.RequestUri);
            return ApiResult<T>.From(ApiResult.Fail(HttpStatusCode.ServiceUnavailable,
                "RaceDay can't reach its event service right now. Please try again in a moment."));
        }

        using (response)
        {
            if (captureSessionCookie && response.IsSuccessStatusCode)
            {
                CaptureApiSessionCookie(response);
            }

            // A 401 on a request that carried a session cookie means the API session has expired.
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(apiCookie) && throwWhenSessionExpired)
            {
                throw new ApiSessionExpiredException();
            }

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object))
                {
                    return ApiResult<T>.Ok(default, response.StatusCode);
                }

                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                    return ApiResult<T>.Ok(data, response.StatusCode);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Unexpected response body from {Url}.", request.RequestUri);
                    return ApiResult<T>.From(ApiResult.Fail(HttpStatusCode.BadGateway, "RaceDay received an unexpected response. Please try again."));
                }
            }

            return ApiResult<T>.From(await ReadErrorAsync(response));
        }
    }

    /// <summary>Extracts "name=value" of the API session cookie from Set-Cookie and keeps it in the MVC session.</summary>
    private void CaptureApiSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return;
        }

        var cookie = setCookies
            .Select(header => header.Split(';', 2)[0].Trim())
            .FirstOrDefault(pair => pair.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal));

        if (cookie != null)
        {
            Session?.SetString(SessionKeys.ApiSessionCookie, cookie);
        }
    }

    /// <summary>Turns an API error response ({ message } or a validation ProblemDetails) into a friendly result.</summary>
    private static async Task<ApiResult> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Some of the details entered are not valid.",
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "That item could not be found. It may have been removed.",
            HttpStatusCode.Conflict => "That change conflicts with existing data.",
            HttpStatusCode.RequestEntityTooLarge => "That file is too large. Images must be 5 MB or smaller.",
            HttpStatusCode.ServiceUnavailable => "A RaceDay service is unavailable right now. Please try again shortly.",
            _ => "Something went wrong while talking to RaceDay. Please try again."
        };

        var fieldErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        string? message = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var json = JsonDocument.Parse(text);
                var root = json.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                    {
                        message = messageElement.GetString();
                    }

                    // ASP.NET Core [ApiController] validation responses list errors per property.
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in errors.EnumerateObject())
                        {
                            var messages = property.Value.ValueKind == JsonValueKind.Array
                                ? property.Value.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(m => m.Length > 0).ToArray()
                                : Array.Empty<string>();
                            var key = property.Name.StartsWith("$.", StringComparison.Ordinal) ? property.Name[2..] : property.Name;
                            fieldErrors[key] = messages;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Non-JSON error bodies (e.g. an HTML error page) are ignored in favour of the fallback message.
        }

        return ApiResult.Fail(response.StatusCode, message ?? fallback, fieldErrors);
    }

    private static string BuildQueryString(EventFilter? filter)
    {
        if (filter == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
            }
        }

        Add("search", filter.Search);
        Add("eventType", filter.EventType);
        Add("location", filter.Location);
        Add("from", filter.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("to", filter.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (filter.UpcomingOnly)
        {
            parts.Add("upcomingOnly=true");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
