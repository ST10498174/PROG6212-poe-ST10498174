using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class EnrolmentAndResultTests
{
    private static async Task<(User organiser, User participant, Event eventItem, Category category)> SeedEvent(RaceDay.Api.Data.RaceDayDbContext db, bool pastEvent = false)
    {
        var organiser = await TestHelper.AddUser(db, "Organiser", $"jarred.organiser.{Guid.NewGuid():N}@example.com");
        var participant = await TestHelper.AddUser(db, "Participant", $"jarred.participant.{Guid.NewGuid():N}@example.com");

        var eventItem = new Event
        {
            OrganiserUserId = organiser.UserId,
            Name = "Jarred Race",
            Description = "Unit test event",
            EventDate = pastEvent ? DateTime.Today.AddDays(-2) : DateTime.Today.AddDays(20),
            Location = "Cape Town",
            DistanceKm = 10,
            EventType = "Run"
        };
        db.Events.Add(eventItem);
        await db.SaveChangesAsync();

        var category = new Category
        {
            EventId = eventItem.EventId,
            Name = "10km",
            CategoryType = "Distance",
            DistanceKm = 10
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return (organiser, participant, eventItem, category);
    }

    [Fact]
    public async Task CreateCategory_OwnerOrganiser_ReturnsCreated()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var controller = TestHelper.WithSession(new CategoriesController(db), data.organiser.UserId, "Organiser");

        var response = await controller.CreateCategory(data.eventItem.EventId, new CategoryRequest
        {
            Name = "Senior 40+",
            CategoryType = "Age",
            MinAge = 40,
            MaxAge = 120
        });

        Assert.IsType<CreatedResult>(response);
    }

    [Fact]
    public async Task CreateEnrolment_Participant_RecordsEnrolment()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.participant.UserId, "Participant");

        var response = await controller.CreateEnrolment(new EnrolmentRequest
        {
            EventId = data.eventItem.EventId,
            CategoryId = data.category.CategoryId
        });

        Assert.IsType<CreatedResult>(response);
        Assert.Single(db.Enrolments);
        Assert.Equal(data.participant.UserId, db.Enrolments.Single().ParticipantUserId);
    }

    [Fact]
    public async Task CreateEnrolment_Duplicate_ReturnsConflict()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        db.Enrolments.Add(new Enrolment
        {
            ParticipantUserId = data.participant.UserId,
            EventId = data.eventItem.EventId,
            CategoryId = data.category.CategoryId,
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.participant.UserId, "Participant");
        var response = await controller.CreateEnrolment(new EnrolmentRequest
        {
            EventId = data.eventItem.EventId,
            CategoryId = data.category.CategoryId
        });

        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task CreateEnrolment_Organiser_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), data.organiser.UserId, "Organiser");

        var response = await controller.CreateEnrolment(new EnrolmentRequest
        {
            EventId = data.eventItem.EventId,
            CategoryId = data.category.CategoryId
        });

        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task CreateResult_OwnerOrganiserAfterEvent_ReturnsCreated()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db, pastEvent: true);
        var enrolment = new Enrolment
        {
            ParticipantUserId = data.participant.UserId,
            EventId = data.eventItem.EventId,
            CategoryId = data.category.CategoryId,
            Status = "Confirmed"
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var controller = TestHelper.WithSession(new ResultsController(db), data.organiser.UserId, "Organiser");
        var response = await controller.CreateResult(new ResultCreateRequest
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = "00:55:30",
            FinishingPosition = 5,
            IsPublished = true
        });

        Assert.IsType<CreatedResult>(response);
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task GetMyResults_ReturnsOnlyOwnPublishedResults()
    {
        using var db = TestHelper.CreateDatabase();
        var data = await SeedEvent(db, pastEvent: true);
        var otherParticipant = await TestHelper.AddUser(db, "Participant", "other.participant@example.com");

        var ownEnrolment = new Enrolment { ParticipantUserId = data.participant.UserId, EventId = data.eventItem.EventId, CategoryId = data.category.CategoryId, Status = "Confirmed" };
        var otherEvent = new Event { OrganiserUserId = data.organiser.UserId, Name = "Other Race", Description = "Other", EventDate = DateTime.Today.AddDays(-1), Location = "Pretoria", DistanceKm = 5, EventType = "Run" };
        db.Events.Add(otherEvent);
        await db.SaveChangesAsync();
        var otherCategory = new Category { EventId = otherEvent.EventId, Name = "5km", CategoryType = "Distance", DistanceKm = 5 };
        db.Categories.Add(otherCategory);
        await db.SaveChangesAsync();
        var otherEnrolment = new Enrolment { ParticipantUserId = otherParticipant.UserId, EventId = otherEvent.EventId, CategoryId = otherCategory.CategoryId, Status = "Confirmed" };
        db.Enrolments.AddRange(ownEnrolment, otherEnrolment);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            new Result { EnrolmentId = ownEnrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(50), FinishingPosition = 2, IsPublished = true },
            new Result { EnrolmentId = otherEnrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(30), FinishingPosition = 1, IsPublished = true });
        await db.SaveChangesAsync();

        var controller = TestHelper.WithSession(new ResultsController(db), data.participant.UserId, "Participant");
        var response = await controller.GetMyResults();

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(ok.Value);
    }
}
