using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the logged-in user's own profile information.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var accessError = RequireAuthenticated();
        if (accessError != null) return accessError;

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null) return NotFound(new { message = "User profile was not found." });

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.ProfilePictureUrl,
            Role = user.Role!.RoleName
        });
    }

    /// <summary>Updates the logged-in user's own profile information.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(ProfileUpdateRequest request)
    {
        var accessError = RequireAuthenticated();
        if (accessError != null) return accessError;

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null) return NotFound(new { message = "User profile was not found." });

        var email = request.Email.Trim().ToLowerInvariant();
        var duplicateEmail = await _db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId);
        if (duplicateEmail) return Conflict(new { message = "Another account already uses this email address." });

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth.Date;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            Role = user.Role!.RoleName
        });
    }
}
