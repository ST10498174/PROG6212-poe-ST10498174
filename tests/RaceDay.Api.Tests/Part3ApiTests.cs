using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Tests for the API features added in Part 3: Blob Storage uploads, the Organiser dashboard summary,
/// enrolment status management, Participant withdrawal, public event filtering and demo seeding.
/// </summary>
public class Part3ApiTests
{
    // Smallest valid PNG header bytes followed by padding: enough to pass the "magic bytes" image check.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52];

    private static async Task<(User organiser, User participant, Event eventItem, Category category)> SeedEvent(RaceDayDbContext db, int daysFromToday = 20)
    {
        var organiser = await TestHelper.AddUser(db, "Organiser", $"jarred.organiser.{Guid.NewGuid():N}@example.com");
        var participant = await TestHelper.AddUser(db, "Participant", $"jarred.participant.{Guid.NewGuid():N}@example.com");

        var eventItem = new Event
        {
            OrganiserUserId = organiser.UserId,
            Name = "Jarred Coastal Run",
            Description = "Part 3 unit test event",
            EventDate = DateTime.Today.AddDays(daysFromToday),
            Location = "Durban",
            DistanceKm = 10,
            EventType = "Run"
        };
        db.Events.Add(eventItem);
        await db.SaveChangesAsync();

        var category = new Category { EventId = eventItem.EventId, Name = "10km", CategoryType = "Distance", DistanceKm = 10 };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return (organiser, participant, eventItem, category);
    }

    private static UploadsController CreateUploadsController(RaceDayDbContext db, FakeBlobStorageService blobs, int? userId, string? role)
    {
        var controller = new UploadsController(db, blobs, Options.Create(new BlobStorageOptions()), NullLogger<UploadsController>.Instance);
        return TestHelper.WithSession(controller, userId, role);
    }

    private static ImageUploadRequest FileRequest(byte[] bytes, string contentType, string fileName = "banner.png")
    {
        var stream = new MemoryStream(bytes);
        return new ImageUploadRequest
        {
            File = new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            }
        };
    }

    private static JsonElement ToJson(object? value) => JsonSerializer.SerializeToElement(value);

    // ---------------------------------------------------------------- Blob Storage uploads

    [Fact]
    public async Task UploadEventBanner_OwnerOrganiser_StoresBlobAndSavesUrl()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var blobs = new FakeBlobStorageService();
        var controller = CreateUploadsController(db, blobs, data.organiser.UserId, "Organiser");

        var response = await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.Single(blobs.Uploads);
        Assert.Equal("event-banners", blobs.Uploads[0].Container);
        Assert.Equal(blobs.Uploads[0].Url, db.Events.Single().BannerImageUrl);
    }

    [Fact]
    public async Task UploadEventBanner_ReplacingBanner_DeletesPreviousBlob()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        data.eventItem.BannerImageUrl = "http://127.0.0.1:10000/devstoreaccount1/event-banners/old.png";
        await db.SaveChangesAsync();
        var blobs = new FakeBlobStorageService();
        var controller = CreateUploadsController(db, blobs, data.organiser.UserId, "Organiser");

        await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.Contains("http://127.0.0.1:10000/devstoreaccount1/event-banners/old.png", blobs.DeletedUrls);
    }

    [Fact]
    public async Task UploadEventBanner_Participant_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var blobs = new FakeBlobStorageService();
        var controller = CreateUploadsController(db, blobs, data.participant.UserId, "Participant");

        var response = await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Empty(blobs.Uploads);
    }

    [Fact]
    public async Task UploadEventBanner_DifferentOrganiser_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var otherOrganiser = await TestHelper.AddUser(db, "Organiser", "jarred.rival@example.com");
        var controller = CreateUploadsController(db, new FakeBlobStorageService(), otherOrganiser.UserId, "Organiser");

        var response = await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    [Fact]
    public async Task UploadEventBanner_NotAnImage_ReturnsBadRequest()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var blobs = new FakeBlobStorageService();
        var controller = CreateUploadsController(db, blobs, data.organiser.UserId, "Organiser");
        var textPretendingToBePng = "hello jarred, not an image"u8.ToArray();

        var response = await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(textPretendingToBePng, "image/png"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Empty(blobs.Uploads);
    }

    [Fact]
    public async Task UploadEventBanner_StorageUnavailable_Returns503()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var blobs = new FakeBlobStorageService { ThrowOnUpload = true };
        var controller = CreateUploadsController(db, blobs, data.organiser.UserId, "Organiser");

        var response = await controller.UploadEventBanner(data.eventItem.EventId, FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.Equal(503, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Null(db.Events.Single().BannerImageUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_LoggedInParticipant_SavesUrl()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var blobs = new FakeBlobStorageService();
        var controller = CreateUploadsController(db, blobs, data.participant.UserId, "Participant");

        var response = await controller.UploadProfilePicture(FileRequest(PngBytes, "image/png", "jarred.png"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal("profile-pictures", blobs.Uploads.Single().Container);
        Assert.Equal(blobs.Uploads.Single().Url, db.Users.Single(u => u.UserId == data.participant.UserId).ProfilePictureUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_NoSession_ReturnsUnauthorized()
    {
        using var db = TestHelper.CreateDatabase();
        var controller = CreateUploadsController(db, new FakeBlobStorageService(), null, null);

        var response = await controller.UploadProfilePicture(FileRequest(PngBytes, "image/png"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    // ---------------------------------------------------------------- Organiser dashboard + enrolment management

    [Fact]
    public async Task GetMyEvents_Organiser_ReturnsOnlyOwnEventsWithCounts()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        await SeedEvent(db); // another organiser's event that must not be returned
        db.Enrolments.Add(new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" });
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EventsController(db), data.organiser.UserId, "Organiser");

        var response = Assert.IsType<OkObjectResult>(await controller.GetMyEvents());
        var events = ToJson(response.Value);

        Assert.Equal(1, events.GetArrayLength());
        Assert.Equal(1, events[0].GetProperty("EnrolmentCount").GetInt32());
        Assert.Equal(1, events[0].GetProperty("PendingCount").GetInt32());
    }

    [Fact]
    public async Task GetMyEvents_Participant_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var controller = TestHelper.WithSession(new EventsController(db), data.participant.UserId, "Participant");

        var response = await controller.GetMyEvents();

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_OwnerOrganiser_ConfirmsEnrolment()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.organiser.UserId, "Organiser");

        var response = await controller.UpdateEnrolmentStatus(enrolment.EnrolmentId, new EnrolmentStatusRequest { Status = "confirmed" });

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal("Confirmed", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_InvalidStatus_ReturnsBadRequest()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.organiser.UserId, "Organiser");

        var response = await controller.UpdateEnrolmentStatus(enrolment.EnrolmentId, new EnrolmentStatusRequest { Status = "Cancelled" });

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_Participant_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.participant.UserId, "Participant");

        var response = await controller.UpdateEnrolmentStatus(enrolment.EnrolmentId, new EnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Equal("Pending", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task WithdrawEnrolment_OwnUpcomingEnrolment_RemovesIt()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.participant.UserId, "Participant");

        var response = await controller.WithdrawEnrolment(enrolment.EnrolmentId);

        Assert.IsType<NoContentResult>(response);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task WithdrawEnrolment_SomeoneElsesEnrolment_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var otherParticipant = await TestHelper.AddUser(db, "Participant", "jarred.other.runner@example.com");
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Pending" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), otherParticipant.UserId, "Participant");

        var response = await controller.WithdrawEnrolment(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task WithdrawEnrolment_PastEvent_ReturnsConflict()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db, daysFromToday: -3);
        var enrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Confirmed" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.participant.UserId, "Participant");

        var response = await controller.WithdrawEnrolment(enrolment.EnrolmentId);

        Assert.IsType<ConflictObjectResult>(response);
    }

    // ---------------------------------------------------------------- Public endpoints and results

    [Fact]
    public async Task GetEvents_EmptyDatabase_ReturnsEmptyListWithoutLogin()
    {
        using var db = TestHelper.CreateDatabase();
        var controller = TestHelper.WithSession(new EventsController(db));

        var response = Assert.IsType<OkObjectResult>(await controller.GetEvents());

        Assert.Equal(0, ToJson(response.Value).GetArrayLength());
    }

    [Fact]
    public async Task GetEvents_FilterByTypeAndUpcoming_ReturnsMatchingEventsOnly()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        db.Events.Add(new Event { OrganiserUserId = data.organiser.UserId, Name = "Jarred Past Ride", Description = "Old", EventDate = DateTime.Today.AddDays(-10), Location = "Cape Town", DistanceKm = 50, EventType = "Cycle" });
        db.Events.Add(new Event { OrganiserUserId = data.organiser.UserId, Name = "Jarred Future Ride", Description = "New", EventDate = DateTime.Today.AddDays(10), Location = "Cape Town", DistanceKm = 50, EventType = "Cycle" });
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new EventsController(db));

        var response = Assert.IsType<OkObjectResult>(await controller.GetEvents(new EventQuery { EventType = "cycle", UpcomingOnly = true }));
        var events = ToJson(response.Value);

        Assert.Equal(1, events.GetArrayLength());
        Assert.Equal("Jarred Future Ride", events[0].GetProperty("Name").GetString());
    }

    [Fact]
    public async Task GetEvents_InvalidEventType_ReturnsBadRequest()
    {
        using var db = TestHelper.CreateDatabase();
        var controller = TestHelper.WithSession(new EventsController(db));

        var response = await controller.GetEvents(new EventQuery { EventType = "Swim" });

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task GetMyResults_IncludesTotalFinishersForPosition()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db, daysFromToday: -5);
        var rival = await TestHelper.AddUser(db, "Participant", "jarred.rival.runner@example.com");
        var mine = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Confirmed" };
        var theirs = new Enrolment { ParticipantUserId = rival.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Confirmed" };
        db.Enrolments.AddRange(mine, theirs);
        await db.SaveChangesAsync();
        db.Results.AddRange(
            new Result { EnrolmentId = mine.EnrolmentId, FinishTime = TimeSpan.FromMinutes(52), FinishingPosition = 2, IsPublished = true },
            new Result { EnrolmentId = theirs.EnrolmentId, FinishTime = TimeSpan.FromMinutes(45), FinishingPosition = 1, IsPublished = true });
        await db.SaveChangesAsync();
        var controller = TestHelper.WithSession(new ResultsController(db), data.participant.UserId, "Participant");

        var response = Assert.IsType<OkObjectResult>(await controller.GetMyResults());
        var results = ToJson(response.Value);

        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(2, results[0].GetProperty("FinishingPosition").GetInt32());
        Assert.Equal(2, results[0].GetProperty("TotalFinishers").GetInt32());
    }

    // ---------------------------------------------------------------- Demo data

    [Fact]
    public async Task DemoDataSeeder_EmptyDatabase_SeedsBothRolesOnce()
    {
        using var db = TestHelper.CreateDatabase();
        var hasher = new PasswordHasher<User>();

        await DemoDataSeeder.SeedAsync(db, hasher, NullLogger.Instance);
        var userCount = db.Users.Count();
        await DemoDataSeeder.SeedAsync(db, hasher, NullLogger.Instance); // second run must not duplicate anything

        Assert.Equal(userCount, db.Users.Count());
        Assert.True(db.Users.Count(u => u.RoleId == 1) >= 2);
        Assert.True(db.Users.Count(u => u.RoleId == 2) >= 2);
        Assert.True(db.Events.Count() >= 3);
        Assert.True(db.Results.Any());
        Assert.All(db.Results.ToList(), r => Assert.True(r.FinishingPosition > 0));
    }

    [Fact]
    public async Task DemoDataSeeder_UsersAlreadyExist_DoesNothing()
    {
        using var db = TestHelper.CreateDatabase();
        await TestHelper.AddUser(db, "Participant", "jarred.existing@example.com");

        await DemoDataSeeder.SeedAsync(db, new PasswordHasher<User>(), NullLogger.Instance);

        Assert.Single(db.Users);
        Assert.Empty(db.Events);
    }
}
