using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ValidParticipant_CreatesUserWithHashedPassword()
    {
        using var db = TestHelper.CreateDatabase();
        var controller = TestHelper.WithSession(new AuthController(db, new PasswordHasher<User>()));

        var request = new RegisterRequest
        {
            FirstName = "Jarred",
            LastName = "UnitTest",
            Email = "jarred@example.com",
            Password = "Password123",
            PhoneNumber = "0825550100",
            DateOfBirth = new DateTime(2002, 5, 10),
            Role = "Participant"
        };

        var response = await controller.Register(request);

        Assert.IsType<CreatedAtActionResult>(response);
        var savedUser = await db.Users.SingleAsync(u => u.Email == "jarred@example.com");
        Assert.NotEqual("Password123", savedUser.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        using var db = TestHelper.CreateDatabase();
        await TestHelper.AddUser(db, "Participant", "jarred@example.com");
        var controller = TestHelper.WithSession(new AuthController(db, new PasswordHasher<User>()));

        var request = new RegisterRequest
        {
            FirstName = "Jarred",
            LastName = "Duplicate",
            Email = "jarred@example.com",
            Password = "Password123",
            PhoneNumber = "0825550100",
            DateOfBirth = new DateTime(2002, 5, 10),
            Role = "Participant"
        };

        var response = await controller.Register(request);
        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task Login_ValidCredentials_CreatesSession()
    {
        using var db = TestHelper.CreateDatabase();
        var user = await TestHelper.AddUser(db, "Organiser", "jarred.organiser@example.com");
        var controller = TestHelper.WithSession(new AuthController(db, new PasswordHasher<User>()));

        var response = await controller.Login(new LoginRequest
        {
            Email = user.Email,
            Password = "Password123"
        });

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal(user.UserId, controller.HttpContext.Session.GetInt32("UserId"));
        Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var db = TestHelper.CreateDatabase();
        var user = await TestHelper.AddUser(db, "Participant", "jarred.participant@example.com");
        var controller = TestHelper.WithSession(new AuthController(db, new PasswordHasher<User>()));

        var response = await controller.Login(new LoginRequest
        {
            Email = user.Email,
            Password = "WrongPassword"
        });

        Assert.IsType<UnauthorizedObjectResult>(response);
    }
}
