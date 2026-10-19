using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventControllerTests
{
    private static EventRequest ValidRequest() => new()
    {
        Name = "Jarred Test Run",
        Description = "A test road running event.",
        Date = DateTime.Today.AddDays(30),
        Location = "Johannesburg, Gauteng",
        DistanceKm = 10,
        EventType = "Run"
    };

    [Fact]
    public async Task CreateEvent_Organiser_ReturnsCreated()
    {
        using var db = TestHelper.CreateDatabase();
        var organiser = await TestHelper.AddUser(db, "Organiser", "jarred.org@example.com");
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserId, "Organiser");

        var response = await controller.CreateEvent(ValidRequest());

        Assert.IsType<CreatedAtActionResult>(response);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task CreateEvent_Participant_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var participant = await TestHelper.AddUser(db, "Participant", "jarred.part@example.com");
        var controller = TestHelper.WithSession(new EventsController(db), participant.UserId, "Participant");

        var response = await controller.CreateEvent(ValidRequest());

        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NoSession_ReturnsUnauthorized()
    {
        using var db = TestHelper.CreateDatabase();
        var controller = TestHelper.WithSession(new EventsController(db));

        var response = await controller.CreateEvent(ValidRequest());

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task UpdateEvent_DifferentOrganiser_ReturnsForbidden()
    {
        using var db = TestHelper.CreateDatabase();
        var owner = await TestHelper.AddUser(db, "Organiser", "owner@example.com");
        var other = await TestHelper.AddUser(db, "Organiser", "jarred.other@example.com");
        var eventItem = new Event
        {
            OrganiserUserId = owner.UserId,
            Name = "Owner Event",
            Description = "Owned by another organiser.",
            EventDate = DateTime.Today.AddDays(20),
            Location = "Durban",
            DistanceKm = 10,
            EventType = "Run"
        };
        db.Events.Add(eventItem);
        await db.SaveChangesAsync();

        var controller = TestHelper.WithSession(new EventsController(db), other.UserId, "Organiser");
        var response = await controller.UpdateEvent(eventItem.EventId, ValidRequest());

        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(403, result.StatusCode);
    }
}
