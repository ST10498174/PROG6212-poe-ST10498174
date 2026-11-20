using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Every operation the MVC front end performs against the Part 2 RaceDay Web API.
/// Controllers depend on this interface so they contain no HTTP or JSON details.
/// </summary>
public interface IRaceDayApiClient
{
    // Authentication
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password);
    Task<ApiResult> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();

    // Profile (both roles)
    Task<ApiResult<ProfileDto>> GetProfileAsync();
    Task<ApiResult> UpdateProfileAsync(ProfileUpdateRequest request);
    Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync(EventFilter? filter = null);
    Task<ApiResult<EventDto>> GetEventAsync(int eventId);
    Task<ApiResult<List<OrganiserEventDto>>> GetMyEventsAsync();
    Task<ApiResult<CreatedEventResponse>> CreateEventAsync(EventRequest request);
    Task<ApiResult> UpdateEventAsync(int eventId, EventRequest request);
    Task<ApiResult> DeleteEventAsync(int eventId);
    Task<ApiResult<ImageUploadResponse>> UploadEventBannerAsync(int eventId, IFormFile file);

    // Categories
    Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId);
    Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult> UpdateCategoryAsync(int categoryId, CategoryRequest request);
    Task<ApiResult> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult<List<MyEnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult> WithdrawAsync(int enrolmentId);
    Task<ApiResult<List<EventEnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, string status);

    // Results
    Task<ApiResult> CreateResultAsync(ResultCreateRequest request);
    Task<ApiResult> UpdateResultAsync(int resultId, ResultUpdateRequest request);
    Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync();
}
