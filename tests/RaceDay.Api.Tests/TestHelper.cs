using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public static class TestHelper
{
    public static RaceDayDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static T WithSession<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var context = new DefaultHttpContext();
        context.Session = new TestSession();

        if (userId.HasValue)
        {
            context.Session.SetInt32("UserId", userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            context.Session.SetString("Role", role);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    public static async Task<User> AddUser(RaceDayDbContext db, string roleName, string email, string password = "Password123")
    {
        var role = await db.Roles.FirstAsync(r => r.RoleName == roleName);
        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = "Jarred",
            LastName = "TestUser",
            Email = email,
            PhoneNumber = "0825550000",
            DateOfBirth = new DateTime(2000, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
